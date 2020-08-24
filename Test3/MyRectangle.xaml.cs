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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Test3
{
    /// <summary>
    /// Interaction logic for MyRectangle.xaml
    /// </summary>
    public partial class MyRectangle : UserControl
    {
        TextBlock ContentTextBlock;

        public MyRectangle()
        {
            InitializeComponent();
            Loaded += MyRectangle_Loaded;
        }

        private void MyRectangle_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTemplate();
            ContentTextBlock = Template.FindName("ContentTextBlock", this) as TextBlock;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(ContentTextBlock.Text) ? GetType().ToString() : ContentTextBlock.Text;
        }
    }
}
