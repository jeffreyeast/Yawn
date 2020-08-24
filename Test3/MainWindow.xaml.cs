//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
using System.Xml.Serialization;
using Yawn;

namespace Test3
{
    public enum ContentTypes
    {
        DockingChoice,
        Rectangle,
        TextBlock,
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly string BinarySaveFilename = "temp.layout_binary";
        static readonly string XmlSaveFilename = "temp.layout_xml";



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Clear_Handler(object sender, RoutedEventArgs e)
        {
            MyDock.Clear();
        }

        private FrameworkElement MyContentCreator(string typeName, string description)
        {
            if (description == ContentTypes.DockingChoice.ToString())
            {
                Grid grid = new Grid();

                StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Horizontal, };

                stackPanel.Children.Add(new DockingChoicePanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Orientation = DockingChoicePanel.Orientations.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                stackPanel.Children.Add(new DockingChoicePanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Orientation = DockingChoicePanel.Orientations.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                stackPanel.Children.Add(new DockingChoicePanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Orientation = DockingChoicePanel.Orientations.Bottom,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                stackPanel.Children.Add(new DockingChoicePanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Orientation = DockingChoicePanel.Orientations.Top,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                stackPanel.Children.Add(new DockingChoicePanel()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Orientation = DockingChoicePanel.Orientations.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                stackPanel.Children.Add(new DockingChoice()
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Visible,
                });

                grid.Children.Add(stackPanel);

                return grid;
            }
            else if (description == ContentTypes.Rectangle.ToString())
            {
                Grid grid = new Grid();

                MyRectangle rect = new MyRectangle()
                {
                    Background = Brushes.Yellow,
                    Foreground = Brushes.Red,
                    Height = 50,
                    Width = 50,
                };
                
                Binding textBinding = new Binding()
                {
                    Path = new PropertyPath("(yawn:DockingPanel.TabText)"),
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Grid), 1),
                };
                rect.SetBinding(TextBlock.TextProperty, textBinding);
                grid.Children.Add(rect);

                return grid;
            }
            else if (description == ContentTypes.TextBlock.ToString())
            {
                return new TextBlock()
                {
                    Text = "Look for orange foreground in Dock tab",
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Restore_Handler(object sender, RoutedEventArgs e)
        {
            using (FileStream stream = new FileStream(BinarySaveFilename, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                object layout = formatter.Deserialize(stream);
                MyDock.Clear();
                Yawn.Layout.Restore(layout, MyDock, MyContentCreator);
            }
        }


        private void RestoreXML_Handler(object sender, RoutedEventArgs e)
        {
            using (StreamReader stream = new StreamReader(XmlSaveFilename))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Yawn.Layout.DockDescriptor));
                object layout = serializer.Deserialize(stream);
                MyDock.Clear();
                Yawn.Layout.Restore(layout, MyDock, MyContentCreator);
            }
        }

        private void Save_Handler(object sender, RoutedEventArgs e)
        {
            using (FileStream stream = new FileStream(BinarySaveFilename, FileMode.Create))
            {
                object layout = Yawn.Layout.Save(MyDock);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, layout);
            }
        }

        private void SaveXML_Handler(object sender, RoutedEventArgs e)
        {
            using (StreamWriter stream = new StreamWriter(XmlSaveFilename))
            {
                object layout = Yawn.Layout.Save(MyDock);
                XmlSerializer serializer = new XmlSerializer(typeof(Yawn.Layout.DockDescriptor));
                serializer.Serialize(stream, layout);
            }
        }

        private void ViewXML_Handler(object sender, RoutedEventArgs e)
        {
            using (StreamReader stream = new StreamReader(XmlSaveFilename))
            {
                string buffer = stream.ReadToEnd();
                XMLDialog dialog = new XMLDialog();
                dialog.XMLTextBlock.Text = buffer;
                dialog.ShowDialog();
            }
        }

        private void Break_Handler(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }
    }
}
