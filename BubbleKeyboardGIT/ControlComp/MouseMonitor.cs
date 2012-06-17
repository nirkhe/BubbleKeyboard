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
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;

namespace ControlCompWithBubbleKeyboard
{
    class MouseMonitor
    {

        private bool LeftPushChoice = false;

        public void setLeftPushChoice(bool n)
        {
            LeftPushChoice = n;
        }

        public MouseMonitor(double clickThreshold)
        {

        }

        public void checkClick(Guesture g)
        {
            if (g != null && g.id == GuestureID.Push)
            {
                Console.WriteLine("Registered a push");
                //Joint lefth = new Joint().ScaleTo(1600, 1200, 0.75f, 0.75f);

                //double theta = nui.NuiCamera.ElevationAngle * Math.PI / 180;

                //SetCursorPos((int)lefth.Position.X,
                //    (int)(lefth.Position.Y * Math.Cos(theta) + lefth.Position.Z * Math.Sin(theta)));
                LeftMouseDown();
                LeftMouseUp();
            }
            
                if (g != null && g.id == GuestureID.Pull)
                {
                    Console.WriteLine("Registered a pull");
                    LeftMouseUp();
                }
            
        }

        public void checkClick2(Guesture g)
        {
            if (g != null && g.id == GuestureID.SwipeRight)
            {
                //Console.WriteLine("Registered a swipe");
                //Joint lefth = new Joint().ScaleTo(1600, 1200, 0.75f, 0.75f);

                //double theta = nui.NuiCamera.ElevationAngle * Math.PI / 180;

                //SetCursorPos((int)lefth.Position.X,
                //    (int)(lefth.Position.Y * Math.Cos(theta) + lefth.Position.Z * Math.Sin(theta)));
                RightMouseDown();
                RightMouseUp();
            }
            else if (g != null && g.id == GuestureID.SwipeLeft)
            {
                LeftMouseDown();
                LeftMouseUp();
            }
            else if (g != null && g.id == GuestureID.SwipeUp)
                LeftMouseDown();
            else if (g != null && g.id == GuestureID.SwipeDown)
                LeftMouseUp();

        }

        public void KeyboardClick()
        {
            LeftMouseDown();
            LeftMouseUp();
        }

        public bool checkClick3(SkeletonData trackSkeleton, Boolean mouseDown)
        {
            if (LeftPushChoice)
            {
                if (trackSkeleton != null)
                {
                    float lefthandpositionz = 0;
                    float leftshoulderpositionz = 0;
                    foreach (Joint j in trackSkeleton.Joints)
                    {
                        if (j.ID == JointID.HandLeft)
                        {
                            lefthandpositionz = j.Position.Z;
                        }
                        if (j.ID == JointID.ShoulderCenter)
                        {
                            leftshoulderpositionz = j.Position.Z;
                        }
                    }
                    if ((leftshoulderpositionz - lefthandpositionz > .4572))
                    {
                        if (mouseDown)
                        {
                            return true;
                        }
                        else
                        {
                            LeftMouseDown();
                            return true;
                        }
                    }
                    else
                    {
                        if (mouseDown)
                        {
                            LeftMouseUp();
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
                return mouseDown;
            }
            return false;
        }

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;

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

        private void RightMouseDown()
        {
            INPUT Input = new INPUT();
            // right down 
            Input.type = INPUT_MOUSE;
            Input.mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;
            SendInput(1, ref Input, Marshal.SizeOf(Input));


        }

        private void RightMouseUp()
        {
            INPUT Input = new INPUT();
            // right up
            Input.type = INPUT_MOUSE;
            Input.mi.dwFlags = MOUSEEVENTF_RIGHTUP;
            SendInput(1, ref Input, Marshal.SizeOf(Input));
        }
    }
}
