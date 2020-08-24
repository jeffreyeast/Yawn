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
    internal enum DragStates
    {
        NotDragging,
        DragPending,
        Dragging,
    }

    /// <summary>
    /// The DockableContentHeader prefaces DockableContent when the TabPosition is any value but Top. It
    /// provides the user with a menu for manipulating the DockableContent.
    /// </summary>
    public partial class DockableContentHeader : UserControl
    {
        //  DataContext is the content Item from DockableCollection, Tag is the DockableCollection


        DragStates DragState;




        public DockableContentHeader()
        {
            InitializeComponent();
            DragState = DragStates.NotDragging;
        }

        private void MouseLeftButtonDown_Handler(object sender, MouseButtonEventArgs e)
        {
            DragState = DragStates.DragPending;
            (Tag as DockableCollection).Dock.IsBeingDragged = true;

            e.Handled = true;
        }

        private void MouseLeftButtonUp_Handler(object sender, MouseButtonEventArgs e)
        {
            DragState = DragStates.NotDragging;
            e.Handled = true;
        }

        private void MouseMove_Handler(object sender, MouseEventArgs e)
        {
            if (DragState == DragStates.DragPending)
            {
                DragState = DragStates.NotDragging;
                Dispatcher.BeginInvoke((Action)delegate
                {
                    Dock dock = (Tag as DockableCollection).Dock;
                    DragDrop.DoDragDrop((Tag as DockableCollection), (Tag as DockableCollection), DragDropEffects.Move);
                    dock.SetDragDropTarget(null);
                }, System.Windows.Threading.DispatcherPriority.Background);
                e.Handled = true;
            }
        }
    }
}
