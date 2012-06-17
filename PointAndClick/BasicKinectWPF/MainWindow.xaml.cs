using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Diagnostics;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        MouseMonitor monitor = new MouseMonitor(0.70f);
        GuestureTracker tracker = new GuestureTracker(15, 0.05f, 0.05f, 0.2f);
        SkeletonData trackedSkeleton;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        Runtime nui;
        ImageFrame currentVideoFrame = new ImageFrame();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            nui = new Runtime();
            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            nui.NuiCamera.ElevationAngle = 20;

            nui.SkeletonFrameReady +=new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

            Process.Start("C:/Windows/System32/osk.exe");

        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame allSkeletons = e.SkeletonFrame;
            trackedSkeleton = (from s in allSkeletons.Skeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked
                                            select s).FirstOrDefault();
            if (trackedSkeleton != null)
            {
                Joint head;
                Joint handRight;
                Joint handLeft;
                head = setEllipsePosition(headE, trackedSkeleton.Joints[JointID.Head]);
                handRight = setEllipsePosition(righthandE, trackedSkeleton.Joints[JointID.HandRight]);
                setEllipseSize(righthandE, trackedSkeleton.Joints[JointID.HandRight], trackedSkeleton.Joints[JointID.Head]);
                handLeft = setEllipsePosition(lefthandE, trackedSkeleton.Joints[JointID.HandLeft]);
                setEllipseSize(lefthandE, trackedSkeleton.Joints[JointID.HandLeft], trackedSkeleton.Joints[JointID.Head]);

                Joint handLeftScaled = trackedSkeleton.Joints[JointID.HandLeft].ScaleTo(1600, 1200, 0.75f, 0.75f);
                Joint handRightScaled = trackedSkeleton.Joints[JointID.HandRight].ScaleTo(1600, 1200, 0.75f, 0.75f);
                SetCursorPos((int) handLeftScaled.Position.X, (int) handLeftScaled.Position.Y);

                monitor.checkClick((int)handLeftScaled.Position.X, (int)handLeftScaled.Position.Y, (int)handRightScaled.Position.X, (int)handRightScaled.Position.Y);
                Guesture g = tracker.track(trackedSkeleton, trackedSkeleton.Joints[JointID.HandLeft], nui.NuiCamera.ElevationAngle);

                if (g != null && g.id == GuestureID.Push)
                {
                    LeftClick();
                    if (push.Fill != Brushes.Red)
                    {
                        push.Fill = Brushes.Red;
                    }
                    else
                    {
                        push.Fill = Brushes.Blue;
                    }
                }

                //if ((distanceEllipses(lefthandE, righthandE) < righthandE.Width / 2))
                //{

                //    double windowLeft = this.Left;
                //    double windowTop = this.Top;

                //    double canvasLeft = windowLeft + canvas1.Margin.Left;
                //    double canvasTop = windowLeft + canvas1.Margin.Top;

                //    int x = (int)(Canvas.GetLeft(lefthandE) + lefthandE.Width / 2 + canvasLeft);
                //    int y = (int)(Canvas.GetTop(lefthandE) + lefthandE.Height / 2 + canvasTop);


                //    LeftClick();
                //}

                if ((distanceEllipses(lefthandE, goal) < lefthandE.Width / 2) && (distanceEllipses(righthandE, goal) < righthandE.Width / 2))
                {
                    headE.Fill = Brushes.Azure;
                }
                else
                {
                    headE.Fill = Brushes.Green;
                }

                
                Random random = new Random();
                Canvas.SetTop(goal, Canvas.GetTop(goal) + random.Next(-3, 4));
                Canvas.SetLeft(goal, Canvas.GetLeft(goal) + random.Next(-3, 4));

                if (btnTest.IsMouseOver && head.Position.Z - handLeft.Position.Z > 0.65)
                {
                    //LeftClick();
                }
                
                
                
            }
            
        }

        #region InputHandling
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const int INPUT_HARDWARE = 2;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        public struct MOUSEINPUT
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

        void LeftClick ( )
        {  
          INPUT  Input = new INPUT();
          // left down 
          Input.type  = INPUT_MOUSE;
          Input.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
          SendInput(1, ref Input, Marshal.SizeOf(Input));

          // left up
          Input.type      =  INPUT_MOUSE;
          Input.mi.dwFlags  = MOUSEEVENTF_LEFTUP;
          SendInput(1, ref Input, Marshal.SizeOf(Input));
        }
        #endregion


        private double distanceEllipses(Ellipse a, Ellipse b)
        {
            double x = (double) (Canvas.GetLeft(a) + a.Width/2) - (Canvas.GetLeft(b) + b.Width/2);
            double y = (double)(Canvas.GetTop(a) + a.Height / 2) - (Canvas.GetTop(b) + b.Height / 2);
            return Math.Sqrt(Math.Pow(x,2) + Math.Pow(x,2));
        }

        private Joint setEllipsePosition(Ellipse e, Joint j)
        {
            var scaledJoint = j.ScaleTo(320, 240);

            Canvas.SetTop(e, scaledJoint.Position.Y);
            Canvas.SetLeft(e, scaledJoint.Position.X);

            return scaledJoint;

        }

        private void setEllipseSize(Ellipse e, Joint j, Joint refJoint)
        {
            if (j.Position.Z < refJoint.Position.Z)
            {
                e.Width = (refJoint.Position.Z - j.Position.Z) * 120;
                e.Height = (refJoint.Position.Z - j.Position.Z) * 120;
            }

        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            currentVideoFrame = e.ImageFrame;
            image2.Source = e.ImageFrame.ToBitmapSource();
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            Byte[] ColoredBytes = GenerateColoredBytes(e.ImageFrame, currentVideoFrame);

            PlanarImage image = e.ImageFrame.Image;
            image1.Source = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null,
                ColoredBytes, image.Width * PixelFormats.Bgr32.BitsPerPixel / 8); 
        }

        private byte[] GenerateColoredBytes(ImageFrame imageFrame, ImageFrame videoFrame)
        {
            int width = imageFrame.Image.Width;
            int height = imageFrame.Image.Height;

            int videoWidth = videoFrame.Image.Width;
            int videoHeight = videoFrame.Image.Height;

            int videoScaleWidth = videoFrame.Image.Width / width;
            int videoScaleHeight = videoFrame.Image.Height / height;

            Byte[] depthData = imageFrame.Image.Bits;

            Byte[] colorFrame = new byte[imageFrame.Image.Height * imageFrame.Image.Width * 4];

            var depthIndex = 0;
            for (var y = 0; y < height; y++)
            {

                var heightOffset = y * width;
                var videoHeightOffset = y * videoScaleHeight * videoWidth;

                for (var x = 0; x < width; x++)
                {

                    var index = ((width - x - 1) + heightOffset) * 4;
                    var videoIndex = (((videoScaleWidth * x) - 1) + videoHeightOffset) * 4;

                    //var distance = GetDistance(depthData[depthIndex], depthData[depthIndex + 1]);
                    var distance = GetDistanceWithPlayerIndex(depthData[depthIndex], depthData[depthIndex + 1]);

                    if (distance < 800.0)
                    {
                        colorFrame[index] = 0;
                        colorFrame[index + 1] = 0;
                        colorFrame[index + 2] = 0;
                    }
                    else
                    {
                        double fraction = (distance) / (4000.0 - 800.0);
                        colorFrame[index] = (byte)(255 * fraction);
                        colorFrame[index + 1] = (byte)(255 * fraction);
                        colorFrame[index + 2] = (byte)(255 * fraction);
                    }

                    int player = GetPlayerIndex(depthData[depthIndex]);
                    if (player > 0)
                    {
                        double fraction = (distance) / (4000.0 - 800.0);
                        colorFrame[index] = (byte)(255 * (((double)player)) / 7);
                        colorFrame[index + 1] = (byte)(255 * (((double)(player)) / 7));
                        colorFrame[index + 2] = (byte)(255 * fraction);

                        //colorFrame[index] = videoFrame.Image.Bits[videoIndex];
                        //colorFrame[index+1] = videoFrame.Image.Bits[videoIndex+1];
                        //colorFrame[index+2] = videoFrame.Image.Bits[videoIndex+1];
                            
                    }

                    depthIndex += 2;
                }
            }
                return colorFrame;
       
        }

        private int GetPlayerIndex(byte p)
        {
            return (int)p & 7; 
        }

        private int GetDistanceWithPlayerIndex(byte first, byte second)
        {
            return (int)(first >> 3 | second << 5);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (goal.Fill != Brushes.Red)
            {
                goal.Fill = Brushes.Red;
            }
            else
            {
                goal.Fill = Brushes.Purple;
            }
        }

        private bool setCanvasRelativeCursorPos(Canvas c, double x, double y)
        {
            double windowLeft = 0;
            double windowTop = 0;

            double canvasLeft = windowLeft + canvas1.Margin.Left;
            double canvasTop = windowLeft + canvas1.Margin.Top;

            return SetCursorPos((int)(x), (int)(y));
        }
    }
}
