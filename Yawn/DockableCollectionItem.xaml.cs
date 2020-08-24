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
    /// <summary>
    /// Interaction logic for DockableCollectionItem.xaml
    /// </summary>
    public partial class DockableCollectionItem : UserControl, INotifyPropertyChanged
    {
        public static DependencyProperty DockableCollectionProperty =
            DependencyProperty.Register("DockableCollection", typeof(DockableCollection), typeof(DockableCollectionItem));

        public DockableCollection DockableCollection
        {
            get => (DockableCollection)GetValue(DockableCollectionProperty);
            set => SetValue(DockableCollectionProperty, value);
        }

        public bool IsContentVisible
        {
            get => _isContentVisible;
            set
            {
                if (_isContentVisible != value)
                {
                    _isContentVisible = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsContentVisible"));
                }
            }
        }
        bool _isContentVisible;

        public event PropertyChangedEventHandler PropertyChanged;



        public DockableCollectionItem()
        {
            InitializeComponent();

            Loaded += DockableCollectionItem_Loaded;
        }

        private void DockableCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VisibleContent")
            {
                IsContentVisible = DataContext == DockableCollection?.VisibleContent;
            }
        }

        private void DockableCollectionItem_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DockableCollectionItem_Loaded;

            DockableCollection.PropertyChanged += DockableCollection_PropertyChanged;
            IsContentVisible = DataContext == DockableCollection.VisibleContent;
        }
    }
}
