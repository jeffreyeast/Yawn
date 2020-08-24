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
    /// <summary>
    /// Interaction logic for DockChoicePanel.xaml
    /// </summary>
    public partial class DockingChoicePanel : UserControl, INotifyPropertyChanged
    {
        public enum Orientations
        {
            Left,
            Right,
            Top,
            Bottom,
            Center,
            LeftMost,
            RightMost,
            TopMost,
            BottomMost,
        }

        public static DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientations), typeof(DockingChoicePanel));

        public Orientations Orientation
        {
            get => (Orientations)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool IsDragTarget
        {
            get => _isDragTarget;
            private set
            {
                if (_isDragTarget != value)
                {
                    _isDragTarget = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDragTarget"));
                }
            }
        }
        bool _isDragTarget;

        public event PropertyChangedEventHandler PropertyChanged;




        public DockingChoicePanel()
        {
            InitializeComponent();
        }

        public TabPositions DefaultTabPosition(DockableCollection referenceCollection)
        {
            switch (Orientation)
            {
                case Orientations.BottomMost:
                case Orientations.LeftMost:
                case Orientations.RightMost:
                    return TabPositions.Bottom;
                case Orientations.TopMost:
                    return TabPositions.Top;
                default:
                    return referenceCollection.TabPosition;
            }
        }

        private void DragDrop_DragEnter_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1 && (dataFormats[0] == typeof(DockableContentContext).ToString() || dataFormats[0] == typeof(DockableCollection).ToString()))
            {
                IsDragTarget = true;
                if (DataContext is DockableCollection dockableCollection)
                {
                    dockableCollection.Dock.DragTargetStyle = Orientation;
                }
            }
        }

        private void DragDrop_DragLeave_Handler(object sender, DragEventArgs e)
        {
            IsDragTarget = false;
            if (DataContext is DockableCollection dockableCollection)
            {
                dockableCollection.Dock.DragTargetStyle = Orientations.Center;
            }
        }

        private void DragDrop_DragOver_Handler(object sender, DragEventArgs e)
        {
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1 && dataFormats[0] == typeof(DockableContentContext).ToString())
            {
                e.Effects = DragDropEffects.Move;
            }
            else if (dataFormats.Count() == 1 && dataFormats[0] == typeof(DockableCollection).ToString())
            {
                if (Orientation == Orientations.Center)
                {
                    if (DataContext is DockableCollection dockableCollection)
                    {
                        DockableCollection sourceCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                        e.Effects = sourceCollection == dockableCollection ? DragDropEffects.None : DragDropEffects.Move;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void DragDrop_Drop_Handler(object sender, DragEventArgs e)
        {
            IsDragTarget = false;
            DockableCollection dockableCollection = DataContext as DockableCollection;
            dockableCollection.Dock.SetDragDropTarget(null);
            string[] dataFormats = e.Data.GetFormats();

            if (dataFormats.Count() == 1 && dataFormats[0] == typeof(DockableContentContext).ToString())
            {
                DockableContentContext droppedContent = e.Data.GetData(typeof(DockableContentContext).ToString()) as DockableContentContext;

                switch (Orientation)
                {
                    case Orientations.Bottom:
                    case Orientations.BottomMost:
                    case Orientations.Left:
                    case Orientations.LeftMost:
                    case Orientations.Right:
                    case Orientations.RightMost:
                    case Orientations.Top:
                    case Orientations.TopMost:
                        dockableCollection.Dock.DropContent(dockableCollection, droppedContent, this);
                        break;
                    case Orientations.Center:
                        dockableCollection.DragDrop_Drop_Handler(sender, e);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                e.Handled = true;
            }
            else if (dataFormats.Count() == 1 && dataFormats[0] == typeof(DockableCollection).ToString())
            {
                switch (Orientation)
                {
                    case Orientations.Bottom:
                    case Orientations.BottomMost:
                    case Orientations.Left:
                    case Orientations.LeftMost:
                    case Orientations.Right:
                    case Orientations.RightMost:
                    case Orientations.Top:
                    case Orientations.TopMost:
                        DockableCollection droppedCollection = e.Data.GetData(typeof(DockableCollection).ToString()) as DockableCollection;
                        dockableCollection.Dock.DropCollection(dockableCollection, droppedCollection, this);
                        break;
                    case Orientations.Center:
#if false
                        dockableCollection.DockTabs.DragDrop_Drop_Handler(sender, e);
#endif
                        break;
                    default:
                        throw new NotImplementedException();
                }

                e.Handled = true;

            }
        }
    }
}
