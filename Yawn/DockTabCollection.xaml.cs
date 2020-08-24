//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Yawn
{
    /// <summary>
    /// Represents a set of DockingTabs for the content of a DockableCollection (or a set of DockableCollections, for a Dock-level TabCollection).
    /// The actual Items are DockableContentContexts, and are required to be live-sorted by TabSequence. The item containers are the DockingTabs.
    /// </summary>
    public partial class DockTabCollection : ItemsControl, INotifyPropertyChanged
    {
        public static DependencyProperty DockableCollectionProperty =
            DependencyProperty.Register("DockableCollection", typeof(DockableCollection), typeof(DockTabCollection));

        public DockableCollection DockableCollection
        {
            get => (DockableCollection)GetValue(DockableCollectionProperty);
            set => SetValue(DockableCollectionProperty, value);
        }

        public static DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(DockTabCollection));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static DependencyProperty TabPositionProperty =
            DependencyProperty.Register("TabPosition", typeof(System.Windows.Controls.Dock), typeof(DockTabCollection));

        public System.Windows.Controls.Dock TabPosition
        {
            get => (System.Windows.Controls.Dock)GetValue(TabPositionProperty);
            set => SetValue(TabPositionProperty, value);
        }

        public bool IsAnyTabClipped
        {
            get => _isAnyTabClipped;
            set
            {
                if (_isAnyTabClipped != value)
                {
                    _isAnyTabClipped = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsAnyTabClipped"));
                }
            }
        }
        bool _isAnyTabClipped;

        public bool IsEmpty
        {
            get => _isEmpty;
            private set
            {
                if (_isEmpty != value)
                {
                    _isEmpty = value;
                    OnPropertyChanged("IsEmpty");
                }
            }
        }
        bool _isEmpty;

        public bool IsDockLevelTabCollection
        {
            get => _isDockLevelTabCollection;
            set
            {
                if (_isDockLevelTabCollection != value)
                {
                    _isDockLevelTabCollection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDockLevelTabCollection"));
                }
            }
        }
        bool _isDockLevelTabCollection;

        public ListCollectionView HiddenTabsView 
        {
            get => _hiddenTabsView;
            private set
            {
                _hiddenTabsView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HiddenTabsView"));
            }
        }
        ListCollectionView _hiddenTabsView;

        private StackPanel ItemStackPanel
        {
            get
            {
                if (_itemStackPanel == null)
                {
                    _itemStackPanel = Template.FindName("ItemStackPanel", this) as StackPanel;
                    if (_itemStackPanel == null)
                    {
                        _itemStackPanel = Utility.FindItemsPanel(this) as StackPanel;
                    }
                }
                return _itemStackPanel;
            }
        }
        StackPanel _itemStackPanel;

        private Menu ItemMenu;
        private static readonly uint MaximumCardinality = 10000;

        public event PropertyChangedEventHandler PropertyChanged;



        public DockTabCollection()
        {
            InitializeComponent();

            IsEmpty = true;
            IsDockLevelTabCollection = true;

            Loaded += DockTabCollection_Loaded;
        }

        private void DockableCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TabPosition":
                    switch (DockableCollection.TabPosition)
                    {
                        case TabPositions.Bottom:
                            TabPosition = System.Windows.Controls.Dock.Bottom;
                            break;
                        case TabPositions.Top:
                            TabPosition = System.Windows.Controls.Dock.Top;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;
                default:
                    break;
            }
        }

        private void DockTabCollection_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTemplate();
            ItemMenu = Template.FindName("ItemMenu", this) as Menu;

            HiddenTabsView = new ListCollectionView(Items)
            {
                CustomSort = new HiddenTabComparer(),
                Filter = QualifyHiddenTab,
            };

            NormalizeSequences();
            Dispatcher.BeginInvoke((Action)delegate
            {
                HideClippedItems();
            }, System.Windows.Threading.DispatcherPriority.Background);

            if (DockableCollection != null)
            {
                EnableAsDragDropTarget();
            }
        }

        private void DockTabSelected_Handler(object sender, ExecutedRoutedEventArgs e)
        {
            DockableCollection.SetVisibleContent((e.Parameter as DockableContentContext).FrameworkElement);
        }

        public void DragDrop_Drop_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1)
            {
                if (dataFormats[0] == typeof(DockableContentContext).ToString())
                {
                    DockableContentContext sourceContext = e.Data.GetData(typeof(DockableContentContext).ToString()) as DockableContentContext;
                    uint dropSequence = GetDropSequence(e.GetPosition(ItemStackPanel));

                    if (sourceContext.DockableCollection == DockableCollection)
                    {
                        sourceContext.TabSequence = dropSequence;
                        DockableCollection.SetVisibleContent(sourceContext.FrameworkElement);
                    }
                    else
                    {
                        sourceContext.DockableCollection.MoveContent(DockableCollection, sourceContext.FrameworkElement, dropSequence);
                    }
                    NormalizeSequences();
                    DockableCollection.Dock.SetDragDropTarget(null);
                    e.Handled = true;
                }
                else if (dataFormats[0] == typeof(DockableCollection).ToString())
                {
                    DockableCollection sourceCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                    uint dropSequence = GetDropSequence(e.GetPosition(ItemStackPanel));

                    sourceCollection.MoveContent(DockableCollection, dropSequence);
                    NormalizeSequences();
                    DockableCollection.Dock.SetDragDropTarget(null);
                    e.Handled = true;
                }
            }
        }

        private void DragDrop_DragOver_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1)
            {
                if (dataFormats[0] == typeof(DockableContentContext).ToString())
                {
                    DockableContentContext sourceContext = e.Data.GetData(typeof(DockableContentContext).ToString()) as DockableContentContext;
                    if (sourceContext.DockableCollection == DockableCollection)
                    {
                        uint dropIndex = GetDropSequence(e.GetPosition(ItemStackPanel));
                        uint firstUnpinnedIndex = FirstUnpinnedSequence;

                        if ((DockingPanel.GetIsPinned(sourceContext.FrameworkElement) && dropIndex <= firstUnpinnedIndex) ||
                            (!DockingPanel.GetIsPinned(sourceContext.FrameworkElement) && dropIndex >= firstUnpinnedIndex))
                        {
                            e.Effects = DragDropEffects.Move;
                        }
                        else
                        {
                            e.Effects = DragDropEffects.None;
                        }
                    }
                    else
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                    e.Handled = true;
                }
                else if (dataFormats[0] == typeof(DockableCollection).ToString())
                {
                    DockableCollection sourceCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                    e.Effects = sourceCollection == DockableCollection ? DragDropEffects.None : DragDropEffects.Move;
                    e.Handled = true;
                }
            }
        }

        private void EnableAsDragDropTarget()
        {
            if (ItemStackPanel != null)
            {
                ItemStackPanel.AllowDrop = true;
                ItemStackPanel.DragOver += DragDrop_DragOver_Handler;
                ItemStackPanel.Drop += DragDrop_Drop_Handler;
            }
        }

        private uint FirstUnpinnedSequence
        {
            get
            {
                uint sequence = 0;
                foreach (DockableContentContext context in Items)
                {
                    if (!DockingPanel.GetIsPinned(context.FrameworkElement))
                    {
                        return context.TabSequence;
                    }
                    sequence = context.TabSequence;
                }

                return sequence + MaximumCardinality;
            }
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DockingTab();
        }

        private uint GetDropSequence(Point positionPoint)
        {
            Debug.Assert(Items.Count > 0);

            double lowerLimit = 0;

            for (int i = 0; i < Items.Count; i++)
            {
                DockingTab dockingTab = ItemContainerGenerator.ContainerFromItem(Items[i]) as DockingTab;
                if (dockingTab.IsTabClipped)
                {
                    break;
                }
                else
                {
                    double upperLimit;
                    double position;

                    switch (Orientation)
                    {
                        case Orientation.Horizontal:
                            upperLimit = lowerLimit + dockingTab.ActualWidth;
                            position = positionPoint.X;
                            break;
                        case Orientation.Vertical:
#if false
                        upperLimit = lowerLimit + dockingTab.ActualHeight;
#else
                            upperLimit = lowerLimit + dockingTab.ActualWidth;
#endif
                            position = positionPoint.Y;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    if (position >= lowerLimit && position < upperLimit)
                    {
                        return (Items[i] as DockableContentContext).TabSequence - (MaximumCardinality - 1);
                    }

                    lowerLimit = upperLimit;
                }
            }

            return (Items[Items.Count - 1] as DockableContentContext).TabSequence + 1;
        }

        private void HideClippedItems()
        {
            if (IsLoaded && 
                Items.Count > 0)
            {
                //  We need the item containers (DockingTabs) to exist, because clipping is performed based on their size. 
                //  Delay clipping until they've been generated.

                if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    HideClippedItemsInternal();
                }
                else
                {
                    ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
                }
            }
        }

        private void HideClippedItemsInternal()
        {
            double usable;
            double working = 0;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    usable = ActualWidth - ItemMenu.ActualWidth;
                    break;
                case Orientation.Vertical:
                    usable = ActualHeight - ItemMenu.ActualHeight;
                    break;
                default:
                    throw new NotImplementedException();
            }

            IsAnyTabClipped = false;
            bool clipStatusChanged = false;

            foreach (DockableContentContext context in Items)
            {
                DockingTab dockTab = ItemContainerGenerator.ContainerFromItem(context) as DockingTab;
                bool wasTabClipped = dockTab.IsTabClipped;

                switch (Orientation)
                {
                    case Orientation.Horizontal:
                        dockTab.IsTabClipped = IsAnyTabClipped = (IsAnyTabClipped || working + dockTab.DesiredSize.Width >= usable);
                        working += dockTab.DesiredSize.Width;
                        break;
                    case Orientation.Vertical:
                        dockTab.IsTabClipped = IsAnyTabClipped = (IsAnyTabClipped || working + dockTab.DesiredSize.Height >= usable);
                        working += dockTab.DesiredSize.Height;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                clipStatusChanged = clipStatusChanged || wasTabClipped != dockTab.IsTabClipped;
            }

            if (clipStatusChanged)
            {
                HiddenTabsView.Refresh();
            }
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return false;
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
                HideClippedItems();
            }
        }

        private uint LastUnclippedSequence
        {
            get
            {
                uint sequence = MaximumCardinality;

                foreach (DockableContentContext context in Items)
                {
                    if (!(ItemContainerGenerator.ContainerFromItem(context) is DockingTab dockingTab) || dockingTab.IsTabClipped)
                    {
                        return sequence;
                    }
                    sequence = context.TabSequence;
                }

                return sequence;
            }
        }

        internal void MakeTabVisible(DockableContentContext targetContext)
        {
            //  If the tab is already visible, do nothing

            if (!(ItemContainerGenerator.ContainerFromItem(targetContext) is DockingTab dockingTab))
            {
                //  Move the tab to be after the pinned tabs or the end of the visible tabs, whichever is less

                targetContext.TabSequence = Math.Min(LastUnclippedSequence, FirstUnpinnedSequence) - 1;
                NormalizeSequences();
                HideClippedItems();
                InvalidateArrange();
            }
            else if (dockingTab.IsTabClipped)
            {
                //  Move the tab to be after the pinned tabs or the end of the visible tabs, whichever is less

                targetContext.TabSequence = Math.Min(LastUnclippedSequence, FirstUnpinnedSequence) - 1;
                NormalizeSequences();
                HideClippedItems();
                InvalidateArrange();
            }
        }

        private void NormalizeSequences()
        {
            //  Assigns a sort sequence number to each tab. The sequences start at MaximumCardinality, and are
            //  bumped by MaximumCardinality. 

            DockableContentContext[] items = new DockableContentContext[Items.Count];

            Items.CopyTo(items, 0);

            for (int index = items.Count() - 1; index >= 0; index--)
            {
                items[index].TabSequence = (uint)(MaximumCardinality * (index + 1));
            }
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            IsEmpty = Items.Count == 0;
            if (Items.Count > MaximumCardinality)
            {
                throw new NotImplementedException("DockTabCollection does not support more than " + MaximumCardinality.ToString() + " dock tabs.");
            }
            InvalidateArrange();
            base.OnItemsChanged(e);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DockableCollectionProperty)
            {
                if (DockableCollection != null)
                {
                    DockableCollection.PropertyChanged += DockableCollection_PropertyChanged;
                    IsDockLevelTabCollection = false;
                    switch (DockableCollection.TabPosition)
                    {
                        case TabPositions.Bottom:
                            TabPosition = System.Windows.Controls.Dock.Bottom;
                            break;
                        case TabPositions.Top:
                            TabPosition = System.Windows.Controls.Dock.Top;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            base.OnPropertyChanged(e);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool QualifyHiddenTab(object obj)
        {
            DockingTab dockingTab = ItemContainerGenerator.ContainerFromItem(obj) as DockingTab;
            return dockingTab?.IsTabClipped ?? false;
        }

        private void SizeChanged_Handler(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
            {
                foreach (DockableContentContext context in Items)
                {
                    DockingTab dockTab = ItemContainerGenerator.ContainerFromItem(context) as DockingTab;
                    dockTab.IsTabClipped = false;
                }
            }
            HideClippedItems();
        }

        private class HiddenTabComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x is DockableContentContext contextX && y is DockableContentContext contextY)
                {
                    return DockingPanel.GetTabText(contextX.FrameworkElement).CompareTo(DockingPanel.GetTabText(contextY.FrameworkElement));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
