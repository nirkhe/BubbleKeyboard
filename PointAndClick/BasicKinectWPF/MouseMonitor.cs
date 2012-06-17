using System;
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

namespace PointAndClick
{
    class MouseMonitor
    {

        public MouseMonitor(double clickThreshold)
        {

        }

        public void checkClick(int leftX, int leftY, int rightX, int rightY)
        {
            double distance = Math.Sqrt(Math.Pow((rightX - leftX), 2) + Math.Pow((rightY - leftY), 2));
            if (distance < 100)
            {
                LeftMouseDown();
            }
            else
            {
                LeftMouseUp();
            }
        }

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public int dwExtraInfo;
        }

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        private void LeftMouseDown()
        {
            INPUT Input = new INPUT();
            // left down 
            Input.type = INPUT_MOUSE;
            Input.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref Input, Marshal.SizeOf(Input));

            
        }

        private void LeftMouseUp()
        {
            INPUT Input = new INPUT();
            // left up
            Input.type = INPUT_MOUSE;
            Input.mi.dwFlags = MOUSEEVENTF_LEFTUP;
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }
    }
}
