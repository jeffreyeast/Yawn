//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Yawn
{
    /// <summary>
    /// The DockingPanel is the control underlying the Dock control. It's a Panel that holds and organizes the
    /// DockableCollections.
    /// </summary>
    public class DockingPanel : Panel
    {
        /// <summary>
        /// Optional attached property which represents the content's preferred location on the dock.
        /// </summary>
        public static readonly DependencyProperty DockPositionProperty = DependencyProperty.RegisterAttached(
            "DockPosition",
            typeof(System.Windows.Controls.Dock?),
            typeof(DockingPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static System.Windows.Controls.Dock? GetDockPosition(FrameworkElement element)
        {
            return (System.Windows.Controls.Dock?)element.GetValue(DockPositionProperty);
        }
        public static void SetDockPosition(FrameworkElement element, System.Windows.Controls.Dock? dockPosition)
        {
            element.SetValue(DockPositionProperty, dockPosition);
        }

        /// <summary>
        /// Optional attached property which holds descriptive text to be saved when the layout is serialized
        /// </summary>
        public static readonly DependencyProperty DescriptiveTextProperty = DependencyProperty.RegisterAttached(
            "DescriptiveText",
            typeof(string),
            typeof(DockingPanel));

        public static string GetDescriptiveText(FrameworkElement element)
        {
            return (string)element.GetValue(DescriptiveTextProperty);
        }
        public static void SetDescriptiveText(FrameworkElement element, string descriptiveText)
        {
            element.SetValue(DescriptiveTextProperty, descriptiveText);
        }

        /// <summary>
        /// Optional attached property which determines if content is pinned withing its collection
        /// </summary>
        public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.RegisterAttached(
            "IsPinned",
            typeof(bool),
            typeof(DockingPanel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static bool GetIsPinned(FrameworkElement element)
        {
            return (bool)element.GetValue(IsPinnedProperty);
        }
        public static void SetIsPinned(FrameworkElement element, bool isPinned)
        {
            element.SetValue(IsPinnedProperty, isPinned);
        }

        /// <summary>
        /// Optional attached property which holds the text to be displayed on the content's dock tab
        /// </summary>
        public static readonly DependencyProperty TabTextProperty = DependencyProperty.RegisterAttached(
            "TabText",
            typeof(string),
            typeof(DockingPanel));

        public static string GetTabText(FrameworkElement element)
        {
            return (string)element.GetValue(TabTextProperty);
        }
        public static void SetTabText(FrameworkElement element, string tabText)
        {
            element.SetValue(TabTextProperty, tabText);
        }

        /// <summary>
        /// Internal attached property used to hold intermediate layout context.
        /// </summary>
        internal static readonly DependencyProperty LayoutContextProperty = DependencyProperty.RegisterAttached(
            "LayoutContext",
            typeof(LayoutContext),
            typeof(DockingPanel));

        internal static LayoutContext GetLayoutContext(DockableCollection element)
        {
            return (LayoutContext)element.GetValue(LayoutContextProperty);
        }
        internal void SetLayoutContext(DockableCollection element, LayoutContext layoutContext)
        {
            element.SetValue(LayoutContextProperty, layoutContext);
        }


        internal List<LayoutContext> LayoutContexts;
        private HorizontalClient HorizontalClientInstance;
        private VerticalClient VerticalClientInstance;
        internal static readonly Size MinimumChildSize = new Size(75, 75);
        private int MaximumHorizontalDepth
        {
            get
            {
                if (_maximumHorizontalDepth == null)
                {
                    if (LayoutContexts.Count == 0)
                    {
                        _maximumHorizontalDepth = 0;
                    }
                    else
                    {
                        _maximumHorizontalDepth = CalculateMaximumHorizontalDepth(LayoutContext.TopLeftMostChild(LayoutContexts[0]));
                    }
                }
                return _maximumHorizontalDepth.Value;
            }
        }
        int? _maximumHorizontalDepth;
        private int MaximumVerticalDepth
        {
            get
            {
                if (_maximumVerticalDepth == null)
                {
                    if (LayoutContexts.Count == 0)
                    {
                        _maximumVerticalDepth = 0;
                    }
                    else
                    {
                        _maximumVerticalDepth = CalculateMaximumHorizontalDepth(LayoutContext.TopLeftMostChild(LayoutContexts[0]));
                    }
                }
                return _maximumVerticalDepth.Value;
            }
        }
        int? _maximumVerticalDepth;

        private IEnumerable<Silo> HorizontalSilos
        {
            get
            {
                if (_horizontalSilos == null)
                {
                    LayoutContext topLeftChild = LayoutContext.TopLeftMostVisibleChild(LayoutContexts[0]);
                    _horizontalSilos = topLeftChild.BuildHorizontalSilos();
                }
                return _horizontalSilos;
            }
        }
        IEnumerable<Silo> _horizontalSilos;

        private IEnumerable<Silo> VerticalSilos
        {
            get
            {
                if (_verticalSilos == null)
                {
                    LayoutContext topLeftChild = LayoutContext.TopLeftMostVisibleChild(LayoutContexts[0]);
                    _verticalSilos = topLeftChild.BuildVerticalSilos();
                }
                return _verticalSilos;
            }
        }
        IEnumerable<Silo> _verticalSilos;

        internal delegate void ArrangedHandler();
        internal event ArrangedHandler Arranged;



        public DockingPanel()
        {
            LayoutContexts = new List<LayoutContext>();
            HorizontalClientInstance = new HorizontalClient();
            VerticalClientInstance = new VerticalClient();
        }

        private void AddChild(DockableCollection visual)
        {
            LayoutContext layoutContext = new LayoutContext(this, visual);
            SetLayoutContext(visual, layoutContext);
            LayoutContexts.Add(layoutContext);
            InsertByDockPosition(layoutContext);
        }

        private void AdjustPostHorizontalInsertion(LayoutContext newElement, LayoutContext reference)
        {
            if (reference.Width.HasValue)
            {
                double availableWidth = reference.Width.Value;
                if (!newElement.DockableCollection.IsMeasureValid)
                {
                    newElement.DockableCollection.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                newElement.Top = reference.Top;
                newElement.Width = Math.Max(MinimumChildSize.Width, 
                    newElement.DockableCollection.DesiredSize.Width < MinimumChildSize.Width ? availableWidth / 2 : Math.Min(availableWidth / 2, newElement.DockableCollection.DesiredSize.Width));
                reference.Width = reference.Width.Value - newElement.Width.Value;
                newElement.Height = reference.Height;
                reference.DockableCollection.InvalidateMeasure();
            }
        }

        private void AdjustPostVerticalInsertion(LayoutContext newElement, LayoutContext reference)
        {
            if (reference.Height.HasValue)
            {
                double availableHeight = reference.Height.Value;
                if (!newElement.DockableCollection.IsMeasureValid)
                {
                    newElement.DockableCollection.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                newElement.Left = reference.Left;
                newElement.Height = Math.Max(MinimumChildSize.Height, 
                    newElement.DockableCollection.DesiredSize.Height < MinimumChildSize.Height ? availableHeight / 2 : Math.Min(availableHeight / 2, newElement.DockableCollection.DesiredSize.Height));
                reference.Height = reference.Height.Value - newElement.Height.Value;
                newElement.Width = reference.Width;
                reference.DockableCollection.InvalidateMeasure();
            }
        }

        //  The priorities are:
        //  1.  Fill the entire dock space
        //  2.  No element should be less than MinimumChildSize. 
        //  3.  Try to give "fixed sized" elements their DesiredSize
        //  4.  Divvy up the rest among the "stretched" elements
        //
        //  Note that we don't pay heed to existing sizes, which means that resizing the Dock
        //  causes everything to reset to the default (i.e., all resizing using scroll bars is lost). 
        //  If this proves too disconcerting, we'll need to handle that case.
        // 
        //  We make a pass from the left to the right, then from the top to the bottom. 

        private void ArrangeByConvolutedLogic(Size finalSize)
        {
            Size minimumChildSize = new Size(Math.Min(MinimumChildSize.Width, finalSize.Width / MaximumHorizontalDepth),
                                             Math.Min(MinimumChildSize.Height, finalSize.Height / MaximumVerticalDepth));

            foreach (Silo silo in HorizontalSilos)
            {
                AutoPositioner.Run(silo, finalSize.Width, minimumChildSize.Width, HorizontalClientInstance);
            }

            foreach (Silo silo in VerticalSilos)
            {
                AutoPositioner.Run(silo, finalSize.Height, minimumChildSize.Height, VerticalClientInstance);
            }
        }

        private Size ArrangeInternal(Size finalSize, bool finalizeArrangement)
        {
#if POSITIONDUMP
            Debug.Print("ArrangeInternal.finalSize = " + finalSize.ToString());
#endif
            if (LayoutContexts.Count > 0)
            {
                retry:

                foreach (var child in LayoutContexts)
                {
                    if (LayoutContexts.Count > 1 &&
                        child.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.FirstOrDefault() == null &&
                        child.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault() == null &&
                        child.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.FirstOrDefault() == null &&
                        child.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault() == null)
                    {
                        //  The element hasn't been inserted into the peer graph yet.  Do so.

                        InsertByDockPosition(child, System.Windows.Controls.Dock.Bottom);
                    }

                    child.Save();
                    child.ResetPosition();
                }

                ArrangeByConvolutedLogic(finalSize);

#if DEBUG
                try
                {
                    foreach (var layoutContext in LayoutContexts)
                    {
                      //  layoutContext.Validate(finalSize);
                    }
                }
                catch (InvalidOperationException)
                {
                    Debugger.Break();
                    foreach (LayoutContext layoutContext in LayoutContexts)
                    {
                        layoutContext.Restore();
                    }
                    goto retry;
                }
#endif
                //  If this is an actual Arrange pass, finalize the arrangement

                if (finalizeArrangement)
                {
                    foreach (var layoutContext in LayoutContexts)
                    {
                        //  If the DockableCollection's width or height are fixed below our minimum value, release them

                        if (!double.IsNaN(layoutContext.DockableCollection.Height))
                        {
                            if (layoutContext.Height.Value >= DockingPanel.MinimumChildSize.Height)
                            {
                                layoutContext.DockableCollection.Height = layoutContext.Height.Value;
                            }
                            else
                            {
                                layoutContext.ClearSplitterContext(System.Windows.Controls.Dock.Bottom);
                            }
                        }
                        if (!double.IsNaN(layoutContext.DockableCollection.Width))
                        {
                            if (layoutContext.Width.Value >= DockingPanel.MinimumChildSize.Width)
                            {
                                layoutContext.DockableCollection.Width = layoutContext.Width.Value;
                            }
                            else
                            {
                                layoutContext.ClearSplitterContext(System.Windows.Controls.Dock.Right);
                            }
                        }

                        //  And finish laying out the DockableCollection

                        layoutContext.DockableCollection.Arrange(layoutContext.DockableCollection.IsCollapsed ? new Rect() : layoutContext.Bounds);
                        if (layoutContext.DockableCollection.State == DockableCollection.States.Loaded)
                        {
                            layoutContext.DockableCollection.State = DockableCollection.States.Pinned;
                        }
                    }

                    Arranged?.Invoke();
                }

#if POSITIONDUMP
                Dump();
                DumpEdges();
#endif

                //  Return the bounding rectangle for all the children

                return BoundingSize(LayoutContexts);
            }
            else
            {
                return finalSize;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return ArrangeInternal(finalSize, true);
        }

        private Size BoundingSize(IEnumerable<LayoutContext> layoutContexts)
        {
            if (layoutContexts.Count() == 0)
            {
                return new Size(0, 0);
            }
            else
            {
                double bottom = double.MinValue;
                double left = double.MaxValue;
                double right = double.MinValue;
                double top = double.MaxValue;

                foreach (var context in layoutContexts)
                {
                    if (!context.DockableCollection.IsCollapsed)
                    {
                        bottom = Math.Max(bottom, context.Bottom.Value);
                        left = Math.Min(left, context.Left.Value);
                        right = Math.Max(right, context.Right.Value);
                        top = Math.Min(top, context.Top.Value);
                    }
                }

                return new Size(Math.Max(0, right - left), Math.Max(0, bottom - top));
            }
        }

        private static int CalculateMaximumHorizontalDepth(LayoutContext topLeftChild)
        {
            int maxDepth = 0;
            for (LayoutContext layoutContext = topLeftChild; layoutContext != null; layoutContext = layoutContext.NextLeftMostBottomPeer)
            {
                int depth = 1;
                foreach (LayoutContext rightPeer in layoutContext.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
                {
                    int rightSubtree = CalculateMaximumHorizontalDepth(rightPeer);
                    depth = Math.Max(depth, rightSubtree + 1);
                }
                maxDepth = Math.Max(maxDepth, depth);
            }
            return maxDepth;
        }

        private static int CalculateMaximumVerticalDepth(LayoutContext topLeftChild)
        {
            int maxDepth = 0;
            for (LayoutContext layoutContext = topLeftChild; layoutContext != null; layoutContext = layoutContext.NextLeftMostBottomPeer)
            {
                int depth = 1;
                foreach (LayoutContext bottomPeer in layoutContext.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
                {
                    int bottomSubtree = CalculateMaximumVerticalDepth(bottomPeer);
                    depth = Math.Max(depth, bottomSubtree + 1);
                }
                maxDepth = Math.Max(maxDepth, depth);
            }
            return maxDepth;
        }

        private void DockPositionPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue && sender is DockableCollection dockableCollection && InternalChildren.Contains(dockableCollection))
            {
                LayoutContext layoutContext = GetLayoutContext(dockableCollection);
                if (layoutContext != null)
                {
                    //  A likely scenario is the collection was created, added to the Dock's Items collection, then
                    //  the attached DockPosition property was set.  We may or may not, have already arranged the
                    //  element. 
                    //
                    //  A less likely scenario is the user simply set DockPosition to a new value for whatever reason.

                    layoutContext.Remove();
                    InsertByDockPosition(layoutContext);
                    layoutContext.InvalidatePositioning(LayoutContext.PositionClasses.All);
                    InvalidateArrange();
                    InvalidateMeasure();
                }
            }
        }

        private void Dump()
        {
            foreach (var layoutContext in LayoutContexts)
            {
                layoutContext.Dump();
            }
        }

        private void DumpAll()
        {
            foreach (var layoutContext in LayoutContexts)
            {
                layoutContext.DumpAll();
            }
        }

        private void DumpEdges(bool force = false)
        {
            foreach (var layoutContext in LayoutContexts)
            {
                layoutContext.DumpEdges(force);
            }
        }

        private LayoutContext FindMidPoint(double centerX, double centerY)
        {
            foreach (LayoutContext layoutContext in LayoutContexts)
            {
                if (centerX >= layoutContext.Left && centerX < layoutContext.Right && centerY >= layoutContext.Top && centerY < layoutContext.Bottom)
                {
                    return layoutContext;
                }
            }
            return null;
        }

        public void InsertAbove(DockableCollection visual, DockableCollection reference)
        {
            LayoutContext visualContext = GetLayoutContext(visual);
            LayoutContext referenceContext = GetLayoutContext(reference);
            referenceContext.InsertAbove(visualContext);
            AdjustPostVerticalInsertion(visualContext, referenceContext);
        }

        public void InsertBelow(DockableCollection visual, DockableCollection reference)
        {
            LayoutContext visualContext = GetLayoutContext(visual);
            LayoutContext referenceContext = GetLayoutContext(reference);
            referenceContext.InsertBelow(visualContext);
            AdjustPostVerticalInsertion(visualContext, referenceContext);
        }

        private void InsertByDockPosition(LayoutContext layoutContext, System.Windows.Controls.Dock? defaultPosition = null)
        {
            if (LayoutContexts.Count > 1)
            {
                //  See if the visual has a DockPosition property. If so, automatically
                //  position the child.

                LayoutContext reference = (LayoutContexts[0] == layoutContext ? LayoutContexts[1] : LayoutContexts[0]);
                System.Windows.Controls.Dock? dockPosition = GetDockPosition(layoutContext.DockableCollection);
                switch (dockPosition ?? defaultPosition)
                {
                    case null:
                        break;
                    case System.Windows.Controls.Dock.Bottom:
                        layoutContext.DockableCollection.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                        layoutContext.InsertBottom(reference);
                        break;
                    case System.Windows.Controls.Dock.Left:
                        layoutContext.DockableCollection.VerticalContentAlignment = VerticalAlignment.Stretch;
                        layoutContext.InsertLeft(reference);
                        break;
                    case System.Windows.Controls.Dock.Right:
                        layoutContext.DockableCollection.VerticalContentAlignment = VerticalAlignment.Stretch;
                        layoutContext.InsertRight(reference);
                        break;
                    case System.Windows.Controls.Dock.Top:
                        layoutContext.DockableCollection.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                        layoutContext.InsertTop(reference);
                        break;
                    default:
                        throw new ArgumentException("Invalid value: " + (dockPosition.HasValue ? dockPosition.Value.ToString() : "<null>"), "DockPosition");
                }
            }
        }

        internal void InsertByLocation(DockableCollection visual, Rect bounds, System.Windows.Controls.Dock defaultPosition)
        {
            LayoutContext layoutContext = GetLayoutContext(visual);
            LayoutContext reference = (LayoutContexts[0] == layoutContext ? LayoutContexts[1] : LayoutContexts[0]);

            //  If the dock resized (smaller) and the bounds are outside the dock, position the visual at the right or bottom edge

            if (bounds.Left >= ActualWidth)
            {
                layoutContext.InsertRight(reference);
            }
            else if (bounds.Top >= ActualHeight)
            {
                layoutContext.InsertBottom(reference);
            }
            else
            {
                //  Find the visual where the new visual's midpoint was

                double centerX = (bounds.Left + bounds.Right) / 2;
                double centerY = (bounds.Top + bounds.Bottom) / 2;
                LayoutContext midpointReference = FindMidPoint(centerX, centerY);
                if (midpointReference == null)
                {
                    //  The midpoint is outside the dock

                    InsertByDockPosition(layoutContext, defaultPosition);
                }
                else
                {
                    //  Position the visual relative to this reference

                    double referenceX = (midpointReference.Left.Value + midpointReference.Right.Value) / 2;
                    double referenceY = (midpointReference.Top.Value + midpointReference.Bottom.Value) / 2;

                    if (Math.Abs(centerX - referenceX) < MinimumChildSize.Width / 2)
                    {
                        if (centerY < referenceY)
                        {
                            midpointReference.InsertAbove(layoutContext);
                        }
                        else
                        {
                            midpointReference.InsertBelow(layoutContext);
                        }
                    }
                    else if (Math.Abs(centerY - referenceY) < MinimumChildSize.Height / 2)
                    {
                        if (centerX < referenceX)
                        {
                            midpointReference.InsertToLeftOf(layoutContext);
                        }
                        else
                        {
                            midpointReference.InsertToRightOf(layoutContext);
                        }
                    }
                    else
                    {
                        midpointReference.InsertBelow(layoutContext);
                    }
                }
            }
        }

        public void InsertToLeftOf(DockableCollection visual, DockableCollection reference)
        {
            LayoutContext visualContext = GetLayoutContext(visual);
            LayoutContext referenceContext = GetLayoutContext(reference);
            referenceContext.InsertToLeftOf(visualContext);
            AdjustPostHorizontalInsertion(visualContext, referenceContext);
        }

        public void InsertToRightOf(DockableCollection visual, DockableCollection reference)
        {
            LayoutContext visualContext = GetLayoutContext(visual);
            LayoutContext referenceContext = GetLayoutContext(reference);
            referenceContext.InsertToRightOf(visualContext);
            AdjustPostHorizontalInsertion(visualContext, referenceContext);
        }

        internal void InvalidateLogical()
        {
            foreach (LayoutContext layoutContext in LayoutContexts)
            {
                layoutContext.InvalidateLogical();
            }
            InvalidateSilos();
        }

        internal void InvalidatePhysical()
        {
            foreach (LayoutContext layoutContext in LayoutContexts)
            {
                layoutContext.InvalidatePhysical();
            }
            InvalidateSilos();
        }

        internal void InvalidateSilos()
        {
            _horizontalSilos = null;
            _verticalSilos = null;
        }

        internal void InvalidatePositioning(LayoutContext.PositionClasses invalidationClass)
        {
            if ((invalidationClass & LayoutContext.PositionClasses.EveryCollection) == LayoutContext.PositionClasses.EveryCollection)
            {
                foreach (LayoutContext layoutContext in LayoutContexts)
                {
                    layoutContext.InvalidatePositioning(invalidationClass);
                }
            }
            InvalidateMeasure();
            InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (LayoutContexts.Count == 0 && InternalChildren.Count > 0)
            {
                //  There's a goofy case where InternalChildren has been set, but OnVisualChildrenChanged hasn't been called,
                //  and we get called. Then, to add insult to injury, they'll call ArrangeOverride.  Wow.

                foreach (FrameworkElement fe in InternalChildren)
                {
                    fe.Measure(availableSize);
                }
                return availableSize;
            }
            else
            {
                bool isFreshLayout = false;

                foreach (LayoutContext layoutContext in LayoutContexts)
                {
                    if (layoutContext.IsFullyPositioned)
                    {
                        layoutContext.DockableCollection.Measure(layoutContext.Size);
                    }
                    else
                    {
                        isFreshLayout = true;
                        layoutContext.ResetPosition();
                        layoutContext.DockableCollection.Measure(availableSize);
                    }
                }

                //  If everything has already been laid out, we're done. Otherwise, we need to do a preliminary arrangement and then remeasure

                if (isFreshLayout)
                {
                    ArrangeInternal(availableSize, false);
                    foreach (LayoutContext layoutContext in LayoutContexts)
                    {
                        layoutContext.DockableCollection.Measure(layoutContext.DockableCollection.IsCollapsed ? new Size(0, 0) : layoutContext.Size);
                    }
                }

#if POSITIONDUMP
                Debug.Print("After MeasureOverride: AvailableSize=" + availableSize.Width.ToString("F0") + "," + availableSize.Height.ToString("F0"));
                foreach (DockableCollection child in InternalChildren)
                {
                    Debug.Print("    " + GetLayoutContext(child).ToString() + ": Desired Size: " + child.DesiredSize.Width.ToString("F0") + "," + child.DesiredSize.Height.ToString("F0"));
                }
#endif
                switch (InternalChildren.Count)
                {
                    case 0:
                        return new Size(0, 0);
                    case 1:
                        return InternalChildren[0].DesiredSize;
                    default:
                        if (availableSize.Width != double.PositiveInfinity && availableSize.Height != double.PositiveInfinity)
                        {
                            return availableSize;
                        }
                        else
                        {
                            return ArrangeInternal(new Size(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight), false);
                        }
                }
            }
        }

        internal void OnAttachedPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DockPositionProperty)
            {
                DockPositionPropertyChanged(sender, e);
            }
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualAdded is DockableCollection dockableCollection)
            {
                AddChild(dockableCollection);
            }

            if (visualRemoved is DockableCollection dockableCollection1)
            {
                LayoutContext layoutContext = GetLayoutContext(dockableCollection1);
                layoutContext.Remove();
                LayoutContexts.Remove(layoutContext);
                SetLayoutContext(dockableCollection1, null);
            }

            _maximumHorizontalDepth = null;
            _maximumVerticalDepth = null;
            InvalidateArrange();
            InvalidateMeasure();
        }

        internal void ReinsertDockableCollection(DockableCollection dockableCollection, System.Windows.Controls.Dock defaultPosition)
        {
            LayoutContext layoutContext = GetLayoutContext(dockableCollection);
            LayoutContexts.Add(layoutContext);
            InsertByDockPosition(layoutContext, defaultPosition);
        }
    }
}
