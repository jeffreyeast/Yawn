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

namespace Yawn
{
    /// <summary>
    /// Interaction logic for DockingChoiceBorder.xaml
    /// </summary>
    public partial class DockingChoiceBorder : Decorator
    {
        public static DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(DockingChoiceBorder));

        public Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static DependencyProperty BorderBrushProperty =
            DependencyProperty.Register("BorderBrush", typeof(Brush), typeof(DockingChoiceBorder));

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public static DependencyProperty BorderBrushThicknessProperty =
            DependencyProperty.Register("BorderBrushThickness", typeof(Thickness), typeof(DockingChoiceBorder));

        public Thickness BorderBrushThickness
        {
            get => (Thickness)GetValue(BorderBrushThicknessProperty);
            set => SetValue(BorderBrushThicknessProperty, value);
        }


        static readonly int cornerRadius = 5;



        public DockingChoiceBorder()
        {
            InitializeComponent();
        }

        private void DragDrop_DragOver_Handler(object sender, DragEventArgs e)
        {
            if (DataContext is DockableCollection dockableCollection)
            {
                dockableCollection.DragDrop_DragOver_Handler(sender, e);
            }
        }

        private void DragDrop_Drop_Handler(object sender, DragEventArgs e)
        {
            if (DataContext is DockableCollection dockableCollection)
            {
                dockableCollection.DragDrop_Drop_Handler(sender, e);
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            Child.Measure(constraint);
            return new Size(Child.DesiredSize.Width + BorderBrushThickness.Left + BorderBrushThickness.Right, Child.DesiredSize.Height + BorderBrushThickness.Top + BorderBrushThickness.Bottom);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Child is FrameworkElement child)
            {
                double interiorHeight = child.ActualHeight;
                double interiorWidth = child.ActualWidth;
                Thickness interiorMargin;

                // Determine the interior corners of the DockingChoice

                double topBottomHeight = 0;
                double leftRightWidth = 0;
                double bottomTopHeight = 0;
                double rightLeftWidth = 0;

                if (!(Utility.FindItemsPanel(child) is Grid itemsPanel))
                {
                    throw new InvalidOperationException("Cannot locate DockingChoice.ItemsPanel");
                }

                if (Child is FrameworkElement fe)
                {
                    interiorMargin = fe.Margin;
                }
                else
                {
                    throw new NotImplementedException();
                }

                foreach (var dcChild in itemsPanel.Children)
                {
                    if (dcChild is DockingChoicePanel dockingChoicePanel)
                    {
                        switch (dockingChoicePanel.Orientation)
                        {
                            case DockingChoicePanel.Orientations.Bottom:
                            case DockingChoicePanel.Orientations.BottomMost:
                                bottomTopHeight += dockingChoicePanel.ActualHeight;
                                break;
                            case DockingChoicePanel.Orientations.Center:
                                break;
                            case DockingChoicePanel.Orientations.Left:
                            case DockingChoicePanel.Orientations.LeftMost:
                                leftRightWidth += dockingChoicePanel.ActualWidth;
                                break;
                            case DockingChoicePanel.Orientations.Right:
                            case DockingChoicePanel.Orientations.RightMost:
                                rightLeftWidth += dockingChoicePanel.ActualWidth;
                                break;
                            case DockingChoicePanel.Orientations.Top:
                            case DockingChoicePanel.Orientations.TopMost:
                                topBottomHeight += dockingChoicePanel.ActualHeight;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }   
                    else
                    {
                        throw new NotImplementedException("Yawn.DockingChoiceBorder only supports objects of type Yawn.DockingChoicePanel. Encountered " + dcChild.GetType().ToString());
                    }
                }

                double X1 = 0;
                double X2 = leftRightWidth;
                double X3 = interiorWidth + interiorMargin.Left + interiorMargin.Right - rightLeftWidth;
                double X4 = interiorWidth + interiorMargin.Left + interiorMargin.Right;

                double Y1 = 0;
                double Y2 = topBottomHeight;
                double Y3 = interiorHeight + interiorMargin.Top + interiorMargin.Bottom - bottomTopHeight;
                double Y4 = interiorHeight + interiorMargin.Top + interiorMargin.Bottom;

                PathFigure path = new PathFigure()
                {
                    IsClosed = true,
                    IsFilled = true,
                    StartPoint = new Point(X1, Y2),                                                             // P1
                };
                path.Segments.Add(new LineSegment() { Point = new Point(X2 - cornerRadius, Y2), });             // P2
                path.Segments.Add(new LineSegment() { Point = new Point(X2, Y2 - cornerRadius), });             // P3
                path.Segments.Add(new LineSegment() { Point = new Point(X2, Y1), });                            // P4
                path.Segments.Add(new LineSegment() { Point = new Point(X3, Y1), });                            // P5
                path.Segments.Add(new LineSegment() { Point = new Point(X3, Y2 - cornerRadius), });             // P6
                path.Segments.Add(new LineSegment() { Point = new Point(X3 + cornerRadius, Y2), });             // P7
                path.Segments.Add(new LineSegment() { Point = new Point(X4, Y2), });                            // P8
                path.Segments.Add(new LineSegment() { Point = new Point(X4, Y3), });                            // P9
                path.Segments.Add(new LineSegment() { Point = new Point(X3 + cornerRadius, Y3), });             // P10
                path.Segments.Add(new LineSegment() { Point = new Point(X3, Y3 + cornerRadius), });             // P11
                path.Segments.Add(new LineSegment() { Point = new Point(X3, Y4), });                            // P12
                path.Segments.Add(new LineSegment() { Point = new Point(X2, Y4), });                            // P13
                path.Segments.Add(new LineSegment() { Point = new Point(X2, Y3 + cornerRadius), });             // P14
                path.Segments.Add(new LineSegment() { Point = new Point(X2 - cornerRadius, Y3), });             // P15
                path.Segments.Add(new LineSegment() { Point = new Point(X1, Y3), });                            // P16

                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(path);

                Pen pen = new Pen(BorderBrush, BorderBrushThickness.Top);
                drawingContext.DrawGeometry(Background, pen, geometry);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
