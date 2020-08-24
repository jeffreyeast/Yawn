//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Yawn
{
    public partial class DockSplitter : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty DockableCollectionProperty =
            DependencyProperty.Register("DockableCollection", typeof(DockableCollection), typeof(DockSplitter));

        public DockableCollection DockableCollection
        {
            get => (DockableCollection)GetValue(DockableCollectionProperty);
            set => SetValue(DockableCollectionProperty, value);
        }

        public static DependencyProperty DockPositionProperty =
            DependencyProperty.Register("DockPosition", typeof(System.Windows.Controls.Dock), typeof(DockSplitter));

        public System.Windows.Controls.Dock DockPosition
        {
            get => (System.Windows.Controls.Dock)GetValue(DockPositionProperty);
            set => SetValue(DockPositionProperty, value);
        }

        public static DependencyProperty DockWindowProperty =
            DependencyProperty.Register("DockWindow", typeof(DockWindow), typeof(DockSplitter));

        public DockWindow DockWindow
        {
            get => (DockWindow)GetValue(DockWindowProperty);
            set => SetValue(DockWindowProperty, value);
        }

        public enum Orientations
        {
            Horizontal,
            Vertical,
            NE,
            SE,
            SW,
            NW,
        }

        public static DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientations), typeof(DockSplitter));

        public Orientations Orientation
        {
            get => (Orientations)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public enum SplitterModes
        {
            Invalid = 0,
            OnWindowEdge,
            BetweenDockableCollections,
            NotBetweenDockableCollections,
        }
        public SplitterModes Mode
        {
            get => _mode;
            internal set
            {
                if (_mode != value)
                {
                    _mode = value;
                    OnPropertyChanged("Mode");
                }
            }
        }
        SplitterModes _mode;

        public Dock ParentDock => DockableCollection?.Dock;

        enum States
        {
            Normal,
            MouseOver,
            Dragging,
        }

        States State;
        Point StartingPosition;

        public event PropertyChangedEventHandler PropertyChanged;



        public DockSplitter()
        {
            InitializeComponent();

            State = States.Normal;
            Loaded += DockSplitter_Loaded;
        }

        internal void ComputeMode()
        {
            if (DockWindow == null)
            {
                if (DockableCollection == null)
                {
                    Mode = SplitterModes.NotBetweenDockableCollections;
                }
                else
                {
                    Mode = DockingPanel.GetLayoutContext(DockableCollection).IsOnDockEdge(DockPosition) ? SplitterModes.NotBetweenDockableCollections : SplitterModes.BetweenDockableCollections;
                }
            }
            else
            {
                Mode = SplitterModes.OnWindowEdge;
            }
        }

        private void DockSplitter_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DockSplitter_Loaded;
            Unloaded += DockSplitter_Unloaded;

            if (DockableCollection != null)
            {
                LayoutContext layoutContext = DockingPanel.GetLayoutContext(DockableCollection);
                if (layoutContext != null)
                {
                    layoutContext.Edges[DockPosition].EdgeRecomputed += EdgeChangeHandler;
                }
            }
            ComputeMode();
        }

        private void DockSplitter_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= DockSplitter_Unloaded;
            if (DockableCollection != null)
            {
                LayoutContext layoutContext = DockingPanel.GetLayoutContext(DockableCollection);
                if (layoutContext != null)
                {
                    layoutContext.Edges[DockPosition].EdgeRecomputed -= EdgeChangeHandler;
                }
            }
        }

        private void EdgeChangeHandler(object sender, EventArgs e)
        {
            ComputeMode();
        }

        private void MouseEnter_Handler(object sender, MouseEventArgs e)
        {
            if (State == States.Normal)
            {
                State = States.MouseOver;
                e.Handled = true;
            }
        }

        private void MouseLeave_Handler(object sender, MouseEventArgs e)
        {
            State = States.Normal;
            e.Handled = true;
        }

        private void MouseLeftButtonDown_Handler(object sender, MouseButtonEventArgs e)
        {
            if (State == States.MouseOver)
            {
                switch (Mode)
                {
                    case SplitterModes.BetweenDockableCollections:
                        State = States.Dragging;
                        ParentDock.SplitterDragStart(this);
                        StartingPosition = e.GetPosition(ParentDock);
                        Mouse.Capture(this);
                        e.Handled = true;
                        break;

                    case SplitterModes.OnWindowEdge:
                        State = States.Dragging;
                        DockWindow.SplitterDragStart(this);
                        StartingPosition = e.GetPosition(DockWindow.Dock.RootDock);
                        Mouse.Capture(this);
                        e.Handled = true;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void MouseLeftButtonUp_Handler(object sender, MouseButtonEventArgs e)
        {
            if (State == States.Dragging)
            {
                Mouse.Capture(null);
                State = States.MouseOver;
                e.Handled = true;
            }
        }

        private void MouseMove_Handler(object sender, MouseEventArgs e)
        {
            if (State == States.Dragging)
            {
                Point currentPosition;

                switch (Mode)
                {
                    case SplitterModes.BetweenDockableCollections:
                        currentPosition = e.GetPosition(ParentDock);

                        switch (Orientation)
                        {
                            case Orientations.Horizontal:
                                ParentDock.SplitterMoved(this, currentPosition.Y - StartingPosition.Y);
                                break;
                            case Orientations.Vertical:
                                ParentDock.SplitterMoved(this, currentPosition.X - StartingPosition.X);
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        StartingPosition = currentPosition;
                        break;

                    case SplitterModes.OnWindowEdge:
                        currentPosition = e.GetPosition(DockWindow.Dock.RootDock);

                        switch (Orientation)
                        {
                            case Orientations.Horizontal:
                                DockWindow.SplitterMoved(this, 0, currentPosition.Y - StartingPosition.Y);
                                break;
                            case Orientations.Vertical:
                                DockWindow.SplitterMoved(this, currentPosition.X - StartingPosition.X, 0);
                                break;
                            case Orientations.NE:
                            case Orientations.NW:
                            case Orientations.SE:
                            case Orientations.SW:
                                DockWindow.SplitterMoved(this, currentPosition.X - StartingPosition.X, currentPosition.Y - StartingPosition.Y);
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        StartingPosition = currentPosition;
                        break;

                    default:
                        throw new NotImplementedException();
                }
                e.Handled = true;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DockableCollectionProperty && IsLoaded)
            {
                if (e.OldValue != null)
                {
                    LayoutContext layoutContext = DockingPanel.GetLayoutContext(e.OldValue as DockableCollection);
                    if (layoutContext != null)
                    {
                        layoutContext.Edges[DockPosition].EdgeRecomputed -= EdgeChangeHandler;
                    }
                }
                if (e.NewValue != null)
                {
                    LayoutContext layoutContext = DockingPanel.GetLayoutContext(e.NewValue as DockableCollection);
                    if (layoutContext != null)
                    {
                        layoutContext.Edges[DockPosition].EdgeRecomputed += EdgeChangeHandler;
                    }
                }
                ComputeMode();
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
