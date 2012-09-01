using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PointAndClickKeyboard_v6
{
    public class Gesture
    {
        public double _magnitude;
        DateTime _timestamp;
        GestureID _id;
        Joint _guestureSource;

        public Gesture(DateTime timestamp, double magnitude, GestureID id, Joint guestureSource)
        {
            this._timestamp = timestamp;
            this._magnitude = magnitude;
            this._id = id;
            this._guestureSource = guestureSource;
        }

        public double magnitude
        {
            get { return _magnitude; }
        }

        public DateTime timestamp
        {
            get { return _timestamp; }
        }

        public GestureID id
        {
            get { return _id; }
        }

        public Joint guestureSource
        {
            get { return _guestureSource; }
        }
    }

    public enum GestureID
    {
        Invalid, SwipeLeft, SwipeRight, SwipeUp, SwipeDown, Push, Pull, Still, Planar
    }
}
