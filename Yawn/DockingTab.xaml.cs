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
using Yawn.Interfaces;

namespace Yawn
{
    /// <summary>
    /// Interaction logic for DockingTab.xaml
    /// </summary>
    public partial class DockingTab : UserControl, INotifyPropertyChanged
    {
        // The DataContext is set to the Item in the DockTabCollection AND MUST NOT BE CHANGED

        public static DependencyProperty DockTabCollectionProperty =
            DependencyProperty.Register("DockTabCollection", typeof(DockTabCollection), typeof(DockingTab));

        public DockTabCollection DockTabCollection
        {
            get => (DockTabCollection)GetValue(DockTabCollectionProperty);
            set => SetValue(DockTabCollectionProperty, value);
        }

        internal enum DragStates
        {
            NotDragging,
            DragPending,
            Dragging,
        }

        internal DragStates DragState { get; set; }

        public bool IsTabClipped
        {
            get => _isTabClipped;
            set
            {
                if (_isTabClipped != value)
                {
                    _isTabClipped = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsTabClipped"));
                }
            }
        }
        bool _isTabClipped;

        public DockableContentContext DockableContentContext => DataContext as DockableContentContext;

        public event PropertyChangedEventHandler PropertyChanged;



        public DockingTab()
        {
            InitializeComponent();

            IsTabClipped = false;

            Loaded += DockingTab_Loaded;
        }

        private void DockingTab_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DockingTab_Loaded;

            ApplyTemplate();
        }

        private void MouseLeftButtonDown_Handler(object sender, MouseButtonEventArgs e)
        {
            DragState = DragStates.DragPending;
            e.Handled = true;
        }

        private void MouseLeftButtonUp_Handler(object sender, MouseButtonEventArgs e)
        {
            DragState = DragStates.NotDragging;
            DockableCollection dockableCollection = DockableContentContext?.DockableCollection;
            FrameworkElement frameworkElement = DockableContentContext?.FrameworkElement;

            if (dockableCollection != null && frameworkElement != null)
            {

                if (DockTabCollection.IsDockLevelTabCollection)
                {
                    dockableCollection.Uncollapse();

                    // Note that the visual is now detached, so it's DataContext is no longer valid. 
                }

                dockableCollection.Show(frameworkElement);
            }

            e.Handled = true;
        }

        private void MouseMove_Handler(object sender, MouseEventArgs e)
        {
            if (DragState == DragStates.DragPending)
            {
                DragState = DragStates.Dragging;
#if false
                Dispatcher.BeginInvoke((Action)delegate
                {
#endif
                    Dock dock = DockableContentContext.DockableCollection.Dock;
                    DragDrop.DoDragDrop(this, DockableContentContext, DragDropEffects.Move);
                    DragState = DragStates.NotDragging;
                    dock?.SetDragDropTarget(null);
#if false
            }, System.Windows.Threading.DispatcherPriority.Background);
#endif
                e.Handled = true;
            }
        }
    }
}
