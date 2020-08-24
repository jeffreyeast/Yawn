//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Yawn
{
    static internal class Utility
    {
        internal static DockWindow CreateFloatingWindow(Point position, Size contentSize, Dock rootDock)
        {
            DockWindow window = new DockWindow(rootDock)
            {
                Height = contentSize.Height + 6 + 43,
                Left = position.X,
                Owner = Application.Current.MainWindow,
                //SizeToContent = SizeToContent.WidthAndHeight,
                ShowInTaskbar = false,
                Top = position.Y,
                Width = contentSize.Width + 4,
                WindowStyle = WindowStyle.None,
            };

            window.Show();
            return window;
        }

        private static double DistanceSquared(DependencyObject d1, DependencyObject d2)
        {
            if (d1 is Control c1 && d2 is Control c2)
            {
                Point p1 = c1.PointToScreen(new Point(c1.ActualWidth / 2, c1.ActualHeight / 2));
                Point p2 = c2.PointToScreen(new Point(c2.ActualWidth / 2, c2.ActualHeight / 2));
                return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
            }
            else
            {
                return double.MaxValue;
            }
        }

        internal static targetType FindAncestorOfType<targetType>(FrameworkElement root) where targetType : FrameworkElement
        {
            DependencyObject parent = root;

            while (parent != null && parent.GetType() != typeof(targetType))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as targetType;
        }

        internal static DependencyObject FindFirstChildOfType(DependencyObject root, Type targetType)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child.GetType() == targetType)
                {
                    return child;
                }

                DependencyObject target = FindFirstChildOfType(child, targetType);
                if (target != null)
                {
                    return target;
                }
            }
            return null;
        }

        internal static Panel FindItemsPanel(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is Panel panel && panel.IsItemsHost)
                {
                    return panel;
                }

                Panel itemsPanel = FindItemsPanel(child);
                if (itemsPanel != null)
                {
                    return itemsPanel;
                }
            }
            return null;
        }

        internal static DependencyObject FindNearestUIElement(Type targetObjectType, DependencyObject subject, DependencyObject ignore)
        {
            //  Look for the closest DockableCollection (using the mid-points of the panels) to this window

            DependencyObject nearestObject = null;

            foreach (Window window in Application.Current.Windows)
            {
                double distanceSquared = double.MaxValue;
                FindNearestUIElementInternal(window, targetObjectType, subject, ignore, ref nearestObject, ref distanceSquared);
            }

            return nearestObject;
        }

        private static void FindNearestUIElementInternal(DependencyObject root, Type targetObjectType, DependencyObject subject, DependencyObject ignore, ref DependencyObject nearestObject, ref double nearestDistanceSquared)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child.GetType() == targetObjectType && child != ignore)
                {
                    double distanceSquared = DistanceSquared(subject, child);

                    if (nearestObject == null || distanceSquared < nearestDistanceSquared)
                    {
                        nearestObject = child;
                        nearestDistanceSquared = distanceSquared;
                    }
                }
                FindNearestUIElementInternal(child, targetObjectType, subject, ignore, ref nearestObject, ref nearestDistanceSquared);
            }
        }

        internal delegate void ForeachUIElementAction(DependencyObject target);

        internal static void ForeachUIElement(Type targetObjectType, DependencyObject root, ForeachUIElementAction actionCallback)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child.GetType() == targetObjectType)
                {
                    actionCallback(child);
                }
                ForeachUIElement(targetObjectType, child, actionCallback);
            }
        }
    }
}
