using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;

namespace Keyboard_v5
{
    class Bubble
    {
        public Canvas Parent;
        public Point Center;
        public int Radius;
        public Brush Color;

        public Ellipse Ellipse;
        public System.Windows.Controls.Label Label;

        public Bubble(Canvas Parent, Point Center, int Radius, Brush Color, char c)
        {
            this.Parent = Parent;
            this.Center = Center;
            this.Radius = Radius;
            this.Color = Color;

            Point TopLeft = new Point(Center.X - Radius, Center.Y - Radius);

            Label = new System.Windows.Controls.Label();

            Label.Visibility = Visibility.Visible;
            Canvas.SetLeft(Label, TopLeft.X);
            Canvas.SetTop(Label, TopLeft.Y);
            Label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            Label.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            Label.Width = 2 * Radius;
            Label.Height = 2 * Radius;
            Label.FontSize = 24;
            Parent.Children.Add(Label);
            setText(c);

            Ellipse = new Ellipse();

            Ellipse.Visibility = Visibility.Visible;
            Ellipse.Fill = Color;
            Ellipse.Opacity = 0.5;
            Canvas.SetLeft(Ellipse, TopLeft.X);
            Canvas.SetTop(Ellipse, TopLeft.Y);
            Ellipse.Width = 2 * Radius;
            Ellipse.Height = 2 * Radius;
            Ellipse.Stroke = Brushes.Black;
            Parent.Children.Add(Ellipse);

        }

        public char GetCharacter()
        {
            return Label.Content.ToString()[0];
        }

        public void SetColor(Brush Color)
        {
            this.Color = Color;
            Ellipse.Fill = Color;
        }

        public void setText(char c)
        {
            Label.Content = c;
        }

        public char Word()
        {
            return (char)(Label.Content);
        }

        public void RemoveFromParent()
        {
            Parent.Children.Remove(Ellipse);
            Parent.Children.Remove(Label);
            Ellipse.Visibility = Visibility.Hidden;
            Label.Visibility = Visibility.Hidden;
        }

        public int CompareTo(Object obj)
        {
            if (obj.GetType().Equals(this.GetType()))
            {
                Bubble other = (Bubble)(obj);

                if ((char)(this.Label.Content) < (char)(other.Label.Content))
                    return -1;
                else if ((char)(this.Label.Content) == (char)(other.Label.Content))
                    return 0;
                else
                    return 1;
            }
            else
            {
                return 1;
            }
        }
    }
}
