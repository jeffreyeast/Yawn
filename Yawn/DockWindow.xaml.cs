//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
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

namespace Yawn
{
    public partial class DockWindow : Window
    {
        public Dock Dock { get; private set; }
        Dock RootDock;
        DragStates DragState;
        Point DragOrigin;

        static readonly Size MinimumWindowSize = new Size(50, 50);



        public DockWindow(Dock rootDock)
        {
            RootDock = rootDock;
            DragState = DragStates.NotDragging;
            InitializeComponent();

            Loaded += DockWindow_Loaded;
        }

        private void CloseWindowHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void DockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTemplate();
            Dock = Utility.FindNearestUIElement(typeof(Dock), this, null) as Dock;
            Dock.RootDock = RootDock;
            Dock.FloatChoice = FloatChoices.Floating;
            RootDock.FloatingDocks.Add(Dock);
            Dock.DockWindow = this;
            Closed += UnderlyingWindowHasClosed;
        }

        private void MaximizeWindowHandler(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void MinimizeWindowHandler(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MouseDoubleClick_Handler(object sender, MouseButtonEventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    e.Handled = true;
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void MouseLeftButtonDown_Handler(object sender, MouseButtonEventArgs e)
        {
            DragState = DragStates.DragPending;
            Dock.IsBeingDragged = true;
            DragOrigin = e.GetPosition(this);
            e.Handled = true;
        }

        private void MouseLeftButtonUp_Handler(object sender, MouseButtonEventArgs e)
        {
            if (DragState != DragStates.NotDragging)
            {
                DragState = DragStates.NotDragging;
                Dock.IsBeingDragged = false;
                e.Handled = true;
            }
        }

        private void MouseMove_Handler(object sender, MouseEventArgs e)
        {
            if (DragState == DragStates.DragPending)
            {
                DragState = DragStates.Dragging;
            }

            if (DragState == DragStates.Dragging)
            {
                DragState = DragStates.Dragging;
                Point currentLocation = e.GetPosition(this);
                currentLocation = PointToScreen(new Point(currentLocation.X - DragOrigin.X, currentLocation.Y - DragOrigin.Y));
                Left = currentLocation.X;
                Top = currentLocation.Y;
                e.Handled = true;
            }
        }

        private void RestoreWindowHandler(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        internal void SplitterDragStart(DockSplitter dockSplitter)
        {
        }

        internal void SplitterMoved(DockSplitter dockSplitter, double deltaX, double deltaY)
        {
            if (deltaX != 0 || deltaY != 0)
            {
                switch (dockSplitter.Orientation)
                {
                    case DockSplitter.Orientations.Horizontal:
                        switch (dockSplitter.DockPosition)
                        {
                            case System.Windows.Controls.Dock.Bottom:
                                Height = Math.Max(MinimumWindowSize.Height, ActualHeight + deltaY);
                                break;
                            case System.Windows.Controls.Dock.Top:
                                Height = Math.Max(MinimumWindowSize.Height, ActualHeight - deltaY);
                                Top = Math.Max(0, Top + deltaY);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    case DockSplitter.Orientations.NE:
                        Height = Math.Max(MinimumWindowSize.Height, ActualHeight - deltaY);
                        Top = Math.Max(0, Top + deltaY);
                        Width = Math.Max(MinimumWindowSize.Width, ActualWidth + deltaX);
                        break;
                    case DockSplitter.Orientations.NW:
                        Height = Math.Max(MinimumWindowSize.Height, ActualHeight - deltaY);
                        Top = Math.Max(0, Top + deltaY);
                        Width = Math.Max(MinimumWindowSize.Width, ActualWidth - deltaX);
                        Left = Math.Max(0, Left + deltaX);
                        break;
                    case DockSplitter.Orientations.SE:
                        Height = Math.Max(MinimumWindowSize.Height, ActualHeight + deltaY);
                        Width = Math.Max(MinimumWindowSize.Width, ActualWidth + deltaX);
                        break;
                    case DockSplitter.Orientations.SW:
                        Height = Math.Max(MinimumWindowSize.Height, ActualHeight + deltaY);
                        Width = Math.Max(MinimumWindowSize.Width, ActualWidth - deltaX);
                        Left = Math.Max(0, Left + deltaX);
                        break;
                    case DockSplitter.Orientations.Vertical:
                        switch (dockSplitter.DockPosition)
                        {
                            case System.Windows.Controls.Dock.Left:
                                Width = Math.Max(MinimumWindowSize.Width, ActualWidth - deltaX);
                                Left = Math.Max(0, Left + deltaX);
                                break;
                            case System.Windows.Controls.Dock.Right:
                                Width = Math.Max(MinimumWindowSize.Width, ActualWidth + deltaX);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void UnderlyingWindowHasClosed(object sender, EventArgs e)
        {
            Dock.Clear();
            Dock.DockWindow = null;
            RootDock.FloatingDocks.Remove(Dock);
        }
    }
}
