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
using Microsoft.Research.Kinect.Audio;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using System.Diagnostics;

public enum GuestureID
{
    Invalid, SwipeLeft, SwipeRight, SwipeUp, SwipeDown, Push, Pull
}