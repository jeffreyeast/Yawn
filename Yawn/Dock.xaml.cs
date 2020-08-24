//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Yawn.FiltersAndViews;

namespace Yawn
{
    public enum FloatChoices
    {
        Floating,
        Root,
    }

    /// <summary>
    /// The Dock class represents the container which manages how Collections (DockableCollection) of Content (DockableContent)
    /// are displayed. There is one Root Dock, which persists as long as the application wishes to display any of the 
    /// Content it manages. There can be zero or more Floating Docks, which are separate windows, each of which holds its own
    /// Dock, managing Collections of Content. Content (and Collections) can be moved between Docks in the same application.
    /// 
    /// The Layout of a Dock can be saved, persisted, and later Restored. This allows the application to persist a layout configuration
    /// across activations of the application.
    /// </summary>
    public partial class Dock : ItemsControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty DockableCollectionProperty = DependencyProperty.RegisterAttached(
            "DockableCollection",
            typeof(DockableCollection),
            typeof(Dock),
            new FrameworkPropertyMetadata(null));

        public static DockableCollection GetDockableCollection(FrameworkElement element)
        {
            return (DockableCollection)element.GetValue(DockableCollectionProperty);
        }
        internal static void SetDockableCollection(FrameworkElement element, DockableCollection dockableCollection)
        {
            element.SetValue(DockableCollectionProperty, dockableCollection);
        }

        public FloatChoices FloatChoice { get; internal set; }

        public bool IsBeingDragged
        {
            get => _isBeingDragged;
            internal set
            {
                if (_isBeingDragged != value)
                {
                    _isBeingDragged = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBeingDragged"));
                }
            }
        }
        bool _isBeingDragged;

        //  Only valid to be tested in XAML on the RootDock

        public bool IsDragDropActive => DragTargetStyle.HasValue;

        public DockableCollection DragTarget
        {
            get => _dragTarget;
            private set
            {
                if (_dragTarget != value)
                {
                    if (_dragTarget != null)
                    {
                        _dragTarget.DragTargetStyle = null;
                    }
                    _dragTarget = value;
                    if (_dragTarget != null)
                    {
                        _dragTarget.DragTargetStyle = DragTargetStyle;
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DragTarget"));
                }
            }
        }
        DockableCollection _dragTarget;

        public DockingChoicePanel.Orientations? DragTargetStyle
        {
            get => _dragTargetStyle;
            internal set
            {
                if (_dragTargetStyle != value)
                {
                    _dragTargetStyle = value;
                    if (DragTarget != null)
                    {
                        DragTarget.DragTargetStyle = value;
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDragDropActive"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DragTargetStyle"));
                }
            }
        }
        DockingChoicePanel.Orientations? _dragTargetStyle;
        
        public Thickness DockingChoicePosition
        {
            get => _dockingChoicePosition;
            internal set
            {
                if (_dockingChoicePosition != value)
                {
                    _dockingChoicePosition = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DockingChoicePosition"));
                }
            }
        }
        Thickness _dockingChoicePosition;

        public Visibility DockingChoiceVisibility
        {
            get => _dockingChoiceVisibility;
            internal set
            {
                if (_dockingChoiceVisibility != value)
                {
                    _dockingChoiceVisibility = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DockingChoiceVisibility"));
                }
            }
        }
        Visibility _dockingChoiceVisibility;

        public Dock RootDock 
        {
            get => _rootDock;
            internal set
            {
                if (_rootDock != value)
                {
                    _rootDock = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RootDock"));
                }
            }
        }
        Dock _rootDock;

        internal DockingPanel DockingPanel { get; private set; }
        DockingChoice DockingChoice;
        internal Dictionary<System.Windows.Controls.Dock, DockTabCollection> DockTabCollections;
        internal DockWindow DockWindow { get; set; }
        internal List<Dock> FloatingDocks { get; private set; }

        //  Various filtered collections for the UI

        public CollapsedCollectionView CollapsedCollections { get; private set; }
        public VisibleCollectionView VisibleCollections { get; private set; }
        public CollapsedCollectionContentView LeftTabs { get; private set; }
        public CollapsedCollectionContentView RightTabs { get; private set; }
        public CollapsedCollectionContentView BottomTabs { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;



        public Dock()
        {
            FloatChoice = FloatChoices.Root;
            DockingChoicePosition = new Thickness(0);
            DockingChoiceVisibility = Visibility.Hidden;
            DockTabCollections = new Dictionary<System.Windows.Controls.Dock, DockTabCollection>();
            FloatingDocks = new List<Dock>();
            RootDock = this;

            CollapsedCollections = new CollapsedCollectionView(Items);
            VisibleCollections = new VisibleCollectionView(Items);
            LeftTabs = new CollapsedCollectionContentView(Items, System.Windows.Controls.Dock.Left);
            RightTabs = new CollapsedCollectionContentView(Items, System.Windows.Controls.Dock.Right);
            BottomTabs = new CollapsedCollectionContentView(Items, System.Windows.Controls.Dock.Bottom);

            InitializeComponent();

            Loaded += Dock_Loaded;
        }

        private void Dock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DockingPanel.InvalidatePositioning(LayoutContext.PositionClasses.EveryCollection | LayoutContext.PositionClasses.All | LayoutContext.PositionClasses.Resize);
        }

        private void AddDockableCollection(DockableCollection dockableCollection)
        {
            dockableCollection.Dock = this;
        }

        /// <summary>
        /// The Clear method is invoked to disassociate the Dock with its Collections, Content and (for the Root Dock), floating
        /// Docks. Each of these is closed and no longer is displayed.
        /// </summary>
        public void Clear()
        {
            Dock floatingDock;
            while ((floatingDock = FloatingDocks.FirstOrDefault())  != null)
            {
                floatingDock.DockWindow.Close();
            }

            Items.Clear();
        }

        /// <summary>
        /// The Close method closes the window associated with floating Dock. 
        /// </summary>
        public void Close()
        {
            switch (FloatChoice)
            {
                case FloatChoices.Floating:
                    DockWindow.Close();
                    break;
                case FloatChoices.Root:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal delegate void PostFloatingWindowCreationHandler(DockWindow dockWindow);

        internal void CreateFloatingDock(Point position, Size size, PostFloatingWindowCreationHandler handler)
        {
            DockWindow dockWindow = Utility.CreateFloatingWindow(position, size, RootDock);

            //  Note that at this time the window is created, but its visual objects haven't been. 

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, handler, dockWindow);
        }

        internal System.Windows.Controls.Dock? DeriveDockPosition(Rect bounds)
        {
            if (bounds.Left == 0 && bounds.Height == DockingPanel.ActualHeight)
            {
                return System.Windows.Controls.Dock.Left;
            }
            if (bounds.Top == 0 && bounds.Width == DockingPanel.ActualWidth)
            {
                return System.Windows.Controls.Dock.Top;
            }
            if (bounds.Right == DockingPanel.ActualWidth && bounds.Height == DockingPanel.ActualHeight)
            {
                return System.Windows.Controls.Dock.Right;
            }
            if (bounds.Bottom == DockingPanel.ActualHeight && bounds.Width == DockingPanel.ActualWidth)
            {
                return System.Windows.Controls.Dock.Bottom;
            }
            return null;
        }

        private void Dock_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTemplate();
            DockingPanel = Utility.FindItemsPanel(this) as DockingPanel;
            DockingChoice = Template.FindName("DockingChoice", this) as DockingChoice;
            DockTabCollections[System.Windows.Controls.Dock.Bottom] = Template.FindName("BottomDockTabs", this) as DockTabCollection;
            DockTabCollections[System.Windows.Controls.Dock.Left] = Template.FindName("LeftDockTabs", this) as DockTabCollection;
            DockTabCollections[System.Windows.Controls.Dock.Right] = Template.FindName("RightDockTabs", this) as DockTabCollection;

            SizeChanged += Dock_SizeChanged;

            if (RootDock == null)
            {
                if (!(Utility.FindNearestUIElement(typeof(Dock), Application.Current.MainWindow, this) is Dock anyDock))
                {
                    //  We must be the first (root) dock

                    RootDock = this;
                }
                else
                {
                    RootDock = anyDock.RootDock;
                }
            }

            DockingPanel.InvalidatePositioning(LayoutContext.PositionClasses.EveryCollection | LayoutContext.PositionClasses.All);
        }

        internal void DropCollection(DockableCollection referenceDockableCollection, DockableCollection droppedCollection, DockingChoicePanel targetDockingChoicePanel)
        {
            droppedCollection.Dock.Items.Remove(droppedCollection);

            switch (targetDockingChoicePanel.Orientation)
            {
                case DockingChoicePanel.Orientations.Bottom:
                    DockingPanel.SetDockPosition(droppedCollection, null);
                    Items.Add(droppedCollection);
                    droppedCollection.HorizontalContentAlignment = referenceDockableCollection.HorizontalContentAlignment;
                    DockingPanel.InsertBelow(droppedCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.BottomMost:
                    DockingPanel.SetDockPosition(droppedCollection, System.Windows.Controls.Dock.Bottom);
                    Items.Add(droppedCollection);
                    break;
                case DockingChoicePanel.Orientations.Left:
                    DockingPanel.SetDockPosition(droppedCollection, null);
                    Items.Add(droppedCollection);
                    droppedCollection.VerticalContentAlignment = referenceDockableCollection.VerticalContentAlignment;
                    DockingPanel.InsertToLeftOf(droppedCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.LeftMost:
                    DockingPanel.SetDockPosition(droppedCollection, System.Windows.Controls.Dock.Left);
                    Items.Add(droppedCollection);
                    break;
                case DockingChoicePanel.Orientations.Right:
                    DockingPanel.SetDockPosition(droppedCollection, null);
                    Items.Add(droppedCollection);
                    droppedCollection.VerticalContentAlignment = referenceDockableCollection.VerticalContentAlignment;
                    DockingPanel.InsertToRightOf(droppedCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.RightMost:
                    DockingPanel.SetDockPosition(droppedCollection, System.Windows.Controls.Dock.Right);
                    Items.Add(droppedCollection);
                    break;
                case DockingChoicePanel.Orientations.Top:
                    DockingPanel.SetDockPosition(droppedCollection, null);
                    Items.Add(droppedCollection);
                    droppedCollection.HorizontalContentAlignment = referenceDockableCollection.HorizontalContentAlignment;
                    DockingPanel.InsertAbove(droppedCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.TopMost:
                    DockingPanel.SetDockPosition(droppedCollection, System.Windows.Controls.Dock.Top);
                    Items.Add(droppedCollection);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal void DropContent(DockableCollection referenceDockableCollection, DockableContentContext droppedContent, DockingChoicePanel targetDockingChoicePanel)
        {
            DockableCollection newCollection = new DockableCollection()
            {
                HorizontalAlignment = droppedContent.DockableCollection.HorizontalAlignment,
                TabPosition = targetDockingChoicePanel.DefaultTabPosition(referenceDockableCollection),
                VerticalAlignment = droppedContent.DockableCollection.VerticalAlignment,
            };

            switch (targetDockingChoicePanel.Orientation)
            {
                case DockingChoicePanel.Orientations.Bottom:
                    DockingPanel.SetDockPosition(newCollection, null);
                    Items.Add(newCollection);
                    DockingPanel.InsertBelow(newCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.BottomMost:
                    DockingPanel.SetDockPosition(newCollection, System.Windows.Controls.Dock.Bottom);
                    Items.Add(newCollection);
                    break;
                case DockingChoicePanel.Orientations.Left:
                    DockingPanel.SetDockPosition(newCollection, null);
                    Items.Add(newCollection);
                    DockingPanel.InsertToLeftOf(newCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.LeftMost:
                    DockingPanel.SetDockPosition(newCollection, System.Windows.Controls.Dock.Left);
                    Items.Add(newCollection);
                    break;
                case DockingChoicePanel.Orientations.Right:
                    DockingPanel.SetDockPosition(newCollection, null);
                    Items.Add(newCollection);
                    DockingPanel.InsertToRightOf(newCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.RightMost:
                    DockingPanel.SetDockPosition(newCollection, System.Windows.Controls.Dock.Right);
                    Items.Add(newCollection);
                    break;
                case DockingChoicePanel.Orientations.Top:
                    DockingPanel.SetDockPosition(newCollection, null);
                    Items.Add(newCollection);
                    DockingPanel.InsertAbove(newCollection, referenceDockableCollection);
                    break;
                case DockingChoicePanel.Orientations.TopMost:
                    DockingPanel.SetDockPosition(newCollection, System.Windows.Controls.Dock.Top);
                    Items.Add(newCollection);
                    break;
                default:
                    throw new NotImplementedException();
            }

            droppedContent.DockableCollection.MoveContent(newCollection, droppedContent.FrameworkElement, 0);
        }

        internal void InsertByLocation(DockableCollection dockableCollection, Rect bounds, System.Windows.Controls.Dock defaultPosition)
        {
            DockingPanel.InsertByLocation(dockableCollection, bounds, defaultPosition);
        }

        internal void InvalidateLogical()
        {
            DockingPanel?.InvalidateLogical();
        }

        internal void InvalidatePhysical()
        {
            DockingPanel?.InvalidatePhysical();
        }

        internal void InvalidatePositioning(LayoutContext.PositionClasses invalidationClass = LayoutContext.PositionClasses.EveryCollection | LayoutContext.PositionClasses.All)
        {
            DockingPanel?.InvalidatePositioning(invalidationClass);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (object o in e.NewItems)
                    {
                        if (o is DockableCollection dockableCollection)
                        {
                            AddDockableCollection(dockableCollection);
                        }
                        else
                        {
                            throw new ArgumentException("Yawn.Dock only supports items of type Yawn.DockableCollection");
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (object o in e.OldItems)
                    {
                        if (o is DockableCollection dockableCollection)
                        {
                            RemoveDockableCollection(dockableCollection);
                        }
                    }
                    if (Items.Count == 0)
                    {
                        Close();
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (object o in Items)
                    {
                        if (o is DockableCollection dockableCollection)
                        {
                            AddDockableCollection(dockableCollection);
                        }
                        else
                        {
                            throw new ArgumentException("Yawn.Dock only supports items of type Yawn.DockableCollection");
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException("Yawn.Dock does not support the following action on its Items collection: " + e.Action.ToString());
            }
        }

        internal void OnAttachedPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DockingPanel?.OnAttachedPropertyChanged(sender, e);
        }

        internal void ReinsertDockableCollection(DockableCollection dockableCollection, System.Windows.Controls.Dock defaultPosition)
        {
            DockingPanel.ReinsertDockableCollection(dockableCollection, defaultPosition);
        }

        private void RemoveDockableCollection(DockableCollection dockableCollection)
        {
            dockableCollection.Dock = null;
        }

        internal void SetDragDropTarget(DockableCollection dockableCollection)
        {
            if (dockableCollection == null)
            {
                DockingChoiceVisibility = Visibility.Hidden;
                DragTarget = null;
            }
            else
            {
                DragTarget = dockableCollection;
                Point dockableCollectionCenter = PointFromScreen(dockableCollection.CenterRelativeToScreen);
                DockingChoicePosition = new Thickness(Math.Max(0.0, dockableCollectionCenter.X - DockingChoice.ActualWidth / 2),
                                                       Math.Max(0.0, dockableCollectionCenter.Y - DockingChoice.ActualHeight / 2), 0.0, 0.0);
                DockingChoiceVisibility = Visibility.Visible;
            }
        }

        internal void SplitterMoved(DockSplitter splitter, double delta)
        {
            DockingPanel.GetLayoutContext(splitter.DockableCollection).SplitterMoved(splitter.DockPosition, delta);
            DockingPanel.InvalidateMeasure();
        }

        internal void SplitterDragStart(DockSplitter splitter)
        {
        }
    }
}
