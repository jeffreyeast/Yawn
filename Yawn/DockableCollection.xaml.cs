//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Yawn.Interfaces;

namespace Yawn
{

    public enum TabPositions
    {
        Top,
        Bottom,
    }



    /// <summary>
    /// The DockableCollection class represents a set of Content (DockableContent). Each DockableCollection
    /// is managed by a Dock. The Dock may be either a Root Dock or a Floating Dock. Only one Content is visible at
    /// any moment. All content is represented by a Tab. The set of Content managed by a DockableCollection is
    /// represented by the Content Tabs, displayed together on one of the four sides of the Collection.
    /// </summary>
    public partial class DockableCollection : ItemsControl, INotifyPropertyChanged
    {
        /// <summary>
        /// The Desciption dependency property allows the user to associate a string with the collection. This string
        /// is preserved across layout save/restore.
        /// </summary>
        public static DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DockableCollection));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// The TabPosition dependency property determines on which of the four sides of the Collection, its Contents' Tabs
        /// are displayed.
        /// </summary>
        public static DependencyProperty TabPositionProperty =
            DependencyProperty.Register("TabPosition", typeof(TabPositions), typeof(DockableCollection), new PropertyMetadata(TabPositions.Bottom));

        public TabPositions TabPosition
        {
            get => (TabPositions)GetValue(TabPositionProperty);
            set => SetValue(TabPositionProperty, value);
        }

        internal Grid LayoutGrid;
        internal Grid ContentGrid;
        public Dock Dock
        {
            get => _dock;
            internal set
            {
                if (_dock != value)
                {
                    _dock = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Dock"));
                }
            }
        }

        Dock _dock;

        internal Point CenterRelativeToScreen => ContentGrid.PointToScreen(new Point(ContentGrid.ActualWidth / 2, ContentGrid.ActualHeight / 2));

        public int ContentCount
        {
            get => _contentCount;
            internal set
            {
                if (_contentCount != value)
                {
                    _contentCount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ContentCount"));
                }
            }
        }
        int _contentCount;

        public OrderedObservableCollection<DockableContentContext> DockableContentContexts { get; private set; }

        public DockingChoicePanel.Orientations? DragTargetStyle
        {
            get => _dragTargetStyle;
            internal set
            {
                if (_dragTargetStyle != value)
                {
                    _dragTargetStyle = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DragTargetStyle"));
                }
            }
        }
        DockingChoicePanel.Orientations? _dragTargetStyle;

        public bool IsCollapsed => State == States.UnPinnedAndCollapsed || State == States.PinnedAndEmpty || State == States.UnPinnedAndEmpty;

        public enum States
        {
            Constructed,
            Loaded,
            Pinned,
            PinnedAndEmpty,
            UnPinnedAndCollapsed,
            UnPinnedAndEmpty,
            UnPinnedAndVisible,
        }
        public States State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    if (_state == States.UnPinnedAndVisible)
                    {
                        Dispatcher.BeginInvoke((Action)delegate
                        {
                            if (_state == States.UnPinnedAndVisible)
                            {
                                Focus();
                            }
                        }, System.Windows.Threading.DispatcherPriority.Normal);
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("State"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsCollapsed"));
                }
            }
        }
        States _state;

        internal FrameworkElement VisibleContent
        {
            get => _visibleContent;
            set
            {
                if (_visibleContent != value)
                {
                    _visibleContent = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VisibleContent"));
                }
            }
        }
        FrameworkElement _visibleContent;

        DockTabCollection DockTabs;
        internal int Id { get; private set; }
        static int LastId = 0;

        //  UnPinnedAndCollapsed context
        internal System.Windows.Controls.Dock CollapsedTabPosition
        {
            get => _collapsedTabPosition;
            set
            {
                if (_collapsedTabPosition != value)
                {
                    _collapsedTabPosition = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollapsedTabPosition"));
                }
            }
        }
        System.Windows.Controls.Dock _collapsedTabPosition;

        public event System.ComponentModel.CancelEventHandler Closing;
        public event EventHandler Closed;
        public event PropertyChangedEventHandler PropertyChanged;



        public DockableCollection()
        {
            DockableContentContexts = new OrderedObservableCollection<DockableContentContext>();

            InitializeComponent();

            TabPosition = TabPositions.Top;
            State = States.Constructed;

            VisibleContent = null;
            Id = ++LastId;

            Loaded += DockableCollection_Loaded;
        }

        /// <summary>
        /// The Close method closes the Collection, and all the content it displays.
        /// </summary>
        public void Close()
        {
            CancelEventArgs args = new CancelEventArgs();
            Closing?.Invoke(this, args);
            if (args.Cancel)
            {
                return;
            }

            foreach (FrameworkElement frameworkElement in Items)
            {
                if (frameworkElement is IClosableContent closableContent)
                {
                    Items.Remove(frameworkElement);

                    closableContent.OnClosed(EventArgs.Empty);
                }
                else
                {
                    Items.Remove(frameworkElement);
                }
            }

            Dock.Items.Remove(this);

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The CloseContent method removes content from the Collection
        /// </summary>
        public void CloseContent(FrameworkElement frameworkElement)
        {
            if (frameworkElement is IClosableContent closableContent)
            {
                CancelEventArgs args = new CancelEventArgs();
                closableContent.OnClosing(args);
                if (args.Cancel)
                {
                    return;
                }

                Items.Remove(frameworkElement);

                closableContent.OnClosed(EventArgs.Empty);
            }
            else
            {
                Items.Remove(frameworkElement);
            }
        }

        private void CloseContent_Handler(object sender, ExecutedRoutedEventArgs e)
        {
            CloseContent(e.Parameter as FrameworkElement);
        }

        private void Dock_Handler(object sender, ExecutedRoutedEventArgs e)
        {
            DockCollection();
        }

        private void Dock_HandlerCanRun(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Dock.FloatChoice == FloatChoices.Floating;
        }

        private void DockableCollection_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTemplate();
            LayoutGrid = Template.FindName("LayoutGrid", this) as Grid;
            ContentGrid = Template.FindName("ContentGrid", this) as Grid;
            DockTabs = Template.FindName("DockTabs", this) as DockTabCollection;

            switch (State)
            {
                case States.Constructed:
                    State = States.Loaded;
                    break;
                default:
                    break;
            }
        }

        private void DockableCollection_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                switch (State)
                {
                    case States.Constructed:
                    case States.Loaded:
                    case States.Pinned:
                    case States.PinnedAndEmpty:
                    case States.UnPinnedAndCollapsed:
                    case States.UnPinnedAndEmpty:
                        break;
                    case States.UnPinnedAndVisible:
                        if (Dock != null && Dock.Items.Count > 1)
                        {
                            if (DockingPanel.GetDockPosition(this).HasValue)
                            {
                                CollapsedTabPosition = DockingPanel.GetDockPosition(this).Value;
                                if (CollapsedTabPosition == System.Windows.Controls.Dock.Top)
                                {
                                    CollapsedTabPosition = System.Windows.Controls.Dock.Bottom;
                                }
                            }
                            else
                            {
                                CollapsedTabPosition = System.Windows.Controls.Dock.Bottom;
                            }
                            State = States.UnPinnedAndCollapsed;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// The DockCollection method moves a collection in a floating Dock to the Root Dock.
        /// </summary>
        public void DockCollection()
        {
            Dock rootDock = Dock.RootDock;

            switch (Dock.FloatChoice)
            {
                case FloatChoices.Floating:
                    switch (State)
                    {
                        case States.UnPinnedAndCollapsed:
                            State = States.UnPinnedAndVisible;
                            Dock.Items.Remove(this);
                            rootDock.Items.Add(this);
                            Focus();
                            break;
                        default:
                            Dock.Items.Remove(this);
                            rootDock.Items.Add(this);
                            Focus();
                            break;
                    }
                    break;
                case FloatChoices.Root:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal void DragDrop_DragEnter_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1)
            {
                if (dataFormats[0] == typeof(DockableContentContext).ToString())
                {
                    Dock.DragTargetStyle = DockingChoicePanel.Orientations.Center;
                    Dock.SetDragDropTarget(this);
                    e.Handled = true;
                }
                else if (dataFormats[0] == typeof(DockableCollection).ToString())
                {
                    DockableCollection sourceCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                    if (sourceCollection != this)
                    {
                        Dock.DragTargetStyle = DockingChoicePanel.Orientations.Center;
                        Dock.SetDragDropTarget(this);
                        e.Handled = true;
                    }
                }
            }
        }

        internal void DragDrop_DragLeave_Handler(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        internal void DragDrop_DragOver_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1)
            {
                if (dataFormats[0] == typeof(DockableContentContext).ToString())
                {
                    e.Effects = DragDropEffects.Move;
                }
                else if (dataFormats[0] == typeof(DockableCollection).ToString())
                {
                    DockableCollection sourceCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                    e.Effects = sourceCollection == this ? DragDropEffects.None : DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        internal void DragDrop_Drop_Handler(object sender, DragEventArgs e)
        {
            DockTabs.DragDrop_Drop_Handler(sender, e);
        }

        /// <summary>
        /// FloatCollection creates a new window and populates it with a Dock containing this collection.
        /// </summary>
        public void FloatCollection()
        {
            switch (Dock.FloatChoice)
            {
                case FloatChoices.Floating:
                    break;
                case FloatChoices.Root:
                    switch (State)
                    {
                        case States.PinnedAndEmpty:
                        case States.UnPinnedAndCollapsed:
                        case States.UnPinnedAndEmpty:
                            FloatCollection(Dock.DockTabCollections[CollapsedTabPosition].PointToScreen(new Point(0, 0)), DesiredSize);
                            break;
                        default:
                            FloatCollection(PointToScreen(new Point(0, 0)), DockingPanel.GetLayoutContext(this).Size);
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal void FloatCollection(Point position, Size size)
        {
            Dock rootDock;

            switch (State)
            {
                case States.PinnedAndEmpty:
                case States.UnPinnedAndCollapsed:
                case States.UnPinnedAndEmpty:
                    DockingPanel.SetDockPosition(this, System.Windows.Controls.Dock.Top);
                    State = States.UnPinnedAndVisible;
                    break;
                default:
                    break;
            }

            rootDock = Dock.RootDock;
            Dock.Items.Remove(this);

            rootDock.CreateFloatingDock(position, size, (DockWindow dockWindow)=>
            {
                dockWindow.Dock.Items.Add(this);
            });
        }

        private void Float_Handler(object sender, ExecutedRoutedEventArgs e)
        {
            FloatCollection();
        }

        private void Float_HandlerCanRun(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Dock.FloatChoice == FloatChoices.Root;
        }

        private DockableContentContext GetDockableContentContext(FrameworkElement frameworkElement)
        {
            foreach (DockableContentContext context in DockableContentContexts)
            {
                if (context.FrameworkElement == frameworkElement)
                {
                    return context;
                }
            }
            throw new InvalidProgramException("Unable to locate DockableContentContext");
        }

        internal void MoveContent(DockableCollection destinationCollection, FrameworkElement frameworkElement, uint tabSequence)
        {
            MoveContentInternal(destinationCollection, frameworkElement, tabSequence);
            destinationCollection.SetVisibleContent(frameworkElement);
        }

        internal void MoveContent(DockableCollection destinationCollection, uint tabSequence)
        {
            FrameworkElement firstElement = null;
            while (Items.Count > 0)
            {
                FrameworkElement frameworkElement = Items[0] as FrameworkElement;
                if (firstElement == null)
                {
                    firstElement = frameworkElement;
                }    
                MoveContentInternal(destinationCollection, frameworkElement, tabSequence++);
            }

            if (firstElement == null)
            {
                throw new InvalidProgramException("No items to move!");
            }

            destinationCollection.SetVisibleContent(firstElement);
        }

        private void MoveContentInternal(DockableCollection destinationCollection, FrameworkElement frameworkElement, uint tabSequence)
        {
            Items.Remove(frameworkElement);
            destinationCollection.Items.Add(frameworkElement);
            foreach (DockableContentContext context in destinationCollection.DockableContentContexts)
            {
                if (context.FrameworkElement == frameworkElement)
                {
                    context.TabSequence = tabSequence;
                    return;
                }
            }
            throw new InvalidProgramException("Unable to locate DockableContentContext for moved FrameworkElement");
        }

        internal void InvalidatePositioning(LayoutContext.PositionClasses positionClass)
        {
            DockingPanel.GetLayoutContext(this)?.InvalidatePositioning(positionClass);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return false;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DockableCollectionItem();
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Size result = base.MeasureOverride(constraint);
            return result;
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (UIElement element in e.NewItems)
                    {
                        if (element is FrameworkElement frameworkElement)
                        {
                            Dock.SetDockableCollection(frameworkElement, this);
                            DockableContentContexts.Add(new DockableContentContext(this, DockableContentContexts, frameworkElement));
                        }
                        else
                        {
                            throw new ArgumentException("DockableCollection only supports children derived from FrameworkElement");
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (UIElement element in e.OldItems)
                    {
                        foreach (var entry in DockableContentContexts)
                        {
                            if (entry.FrameworkElement == element)
                            {
                                Dock.SetDockableCollection(entry.FrameworkElement, null);
                                DockableContentContexts.Remove(entry);
                                break;
                            }
                        }
                    }
                    break;

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    foreach (var entry in DockableContentContexts)
                    {
                        Dock.SetDockableCollection(entry.FrameworkElement, null);
                    }
                    DockableContentContexts.Clear();
                    foreach (UIElement element in Items)
                    {
                        if (element is FrameworkElement frameworkElement)
                        {
                            Dock.SetDockableCollection(frameworkElement, this);
                            DockableContentContexts.Add(new DockableContentContext(this, DockableContentContexts, frameworkElement));
                        }
                        else
                        {
                            throw new ArgumentException("DockableCollection only supports children derived from FrameworkElement");
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (Items.Count == 0)
            {
                switch (State)
                {
                    case States.Constructed:
                    case States.Loaded:
                    case States.Pinned:
                    case States.PinnedAndEmpty:
                        State = States.PinnedAndEmpty;
                        break;
                    case States.UnPinnedAndCollapsed:
                    case States.UnPinnedAndEmpty:
                    case States.UnPinnedAndVisible:
                        State = States.UnPinnedAndEmpty;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (State)
                {
                    case States.PinnedAndEmpty:
                        State = States.Pinned;
                        break;
                    case States.UnPinnedAndEmpty:
                        State = States.UnPinnedAndVisible;
                        break;
                    default:
                        break;
                }
                if (Items.Count == 1)
                {
                    InvalidatePositioning(LayoutContext.PositionClasses.Internal);
                }
            }

            ContentCount = Items.Count;
            if (VisibleContent != null && !Items.Contains(VisibleContent))
            {
                VisibleContent = null;
            }
            if (VisibleContent == null && ContentCount > 0)
            {
                SetVisibleContent(Items[0] as FrameworkElement);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DockingPanel.DockPositionProperty && Dock != null)
            {
                Dock.OnAttachedPropertyChanged(this, e);
            }
            else if (e.Property == TabPositionProperty)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TabPosition"));
            }
            else if (e.Property == VisibilityProperty)
            {
                OnVisibilityChanged((Visibility)e.OldValue, (Visibility)e.NewValue);
            }
            else if (e.Property == WidthProperty && ((Dock?.DockingPanel?.IsIdle) ?? true))
            {
                LayoutContext layoutContext = DockingPanel.GetLayoutContext(this);
                layoutContext.Size.Width.SetUserValue(Width);
            }
            else if (e.Property == HeightProperty && ((Dock?.DockingPanel?.IsIdle) ?? true))
            {
                LayoutContext layoutContext = DockingPanel.GetLayoutContext(this);
                layoutContext.Size.Height.SetUserValue(Height);
            }
        }

        private void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
        {
            switch (newValue)
            {
                case Visibility.Collapsed:
                case Visibility.Visible:
                    if (DockingPanel.GetLayoutContext(this) is LayoutContext layoutContext)
                    {
                        layoutContext.InvalidatePositioning(LayoutContext.PositionClasses.Collapse | LayoutContext.PositionClasses.Internal);
                    }
                    break;
                default:
                    break;
            }
        }

        internal void SetVisibleContent(FrameworkElement frameworkElement)
        {
            VisibleContent = frameworkElement;
            if (IsLoaded && frameworkElement != null)
            {
                DockTabs.MakeTabVisible(GetDockableContentContext(frameworkElement));
            }
        }

        public void Show(FrameworkElement frameworkElement)
        {
            if (State == States.UnPinnedAndCollapsed)
            {
                State = States.UnPinnedAndVisible;
            }
            SetVisibleContent(frameworkElement);
        }

        private void ShowContent_Handler(object sender, ExecutedRoutedEventArgs e)
        {
            SetVisibleContent(e.Parameter as FrameworkElement);
        }

        private void ShowContent_HandlerCanRun(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = VisibleContent != e.Parameter;
        }

        public override string ToString()
        {
            string result = "";
            string seperator = "";

            foreach (FrameworkElement dockableContent in Items)
            {
                result += seperator + (DockingPanel.GetTabText(dockableContent) ?? dockableContent.ToString());
                seperator = ",";
            }

            return result;
        }

        internal void Uncollapse()
        {
            State = States.UnPinnedAndVisible;
            Focus();
        }


#if true
        private void ItemsControl_MouseEnter(object sender, MouseEventArgs e)
        {
        }

        private void ItemsControl_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void ItemsControl_MouseMove(object sender, MouseEventArgs e)
        {
        }
#else
        public enum HoverStates
        {
            MouseNotWithinObject,
            MouseWithinObject,
            MouseHovering,
        }

        public HoverStates HoverState
        {
            get => _hoverState;
            set
            {
                if (_hoverState != value)
                {
                    _hoverState = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HoverState"));

                    if (_hoverState == HoverStates.MouseHovering)
                    {
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LayoutContext"));
                    }
                }
            }
        }
        HoverStates _hoverState;

        public LayoutContext LayoutContext => DockingPanel.GetLayoutContext(this);

        Timer MouseTimer = new Timer(500);

        private void ItemsControl_MouseEnter(object sender, MouseEventArgs e)
        {
            HoverState = HoverStates.MouseWithinObject;
            MouseTimer.Elapsed += (timerSender, timerE) => { MouseTimer.Stop(); HoverState = HoverStates.MouseHovering;  };
            MouseTimer.Start();
        }

        private void ItemsControl_MouseLeave(object sender, MouseEventArgs e)
        {
            switch (HoverState)
            {
                case HoverStates.MouseHovering:
                    break;
                case HoverStates.MouseNotWithinObject:
                    break;
                case HoverStates.MouseWithinObject:
                    MouseTimer.Stop();
                    break;
                default:
                    throw new NotImplementedException();
            }
            HoverState = HoverStates.MouseNotWithinObject;
        }

        private void ItemsControl_MouseMove(object sender, MouseEventArgs e)
        {
            switch (HoverState)
            {
                case HoverStates.MouseHovering:
                    break;
                case HoverStates.MouseNotWithinObject:
                    break;
                case HoverStates.MouseWithinObject:
                    MouseTimer.Stop();
                    MouseTimer.Start();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
#endif
    }
}
