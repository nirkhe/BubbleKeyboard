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

namespace PointAndClickKeyboard_v6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PointAndClickKeyboard : Window
    {

        SkeletonData TrackedSkeleton;

        Runtime nui;
        ImageFrame currentVideoFrame = new ImageFrame();

        bool Capitals = false, Shift = false;
        string CurrentWord = "";

        string last_char = null;
        DateTime last_char_time = DateTime.Now;

        DateTime StartTime;

        Queue<string> PositionData, WordData, SentenceData, TextData;

        JointID SelectionHand = JointID.HandLeft;
        JointID MotionHand = JointID.HandRight;

        GestureTracker GestureTracker = new GestureTracker(15, 0.05f, 0.05f, 0.05f);

        double SelectionHandDistance = 0.0, MotionHandDistance = 0.0;

        List<System.Windows.Controls.Button> Buttons;


        public PointAndClickKeyboard()
        {
            StartTime = DateTime.Now;
            PositionData = new Queue<string>();
            WordData = new Queue<string>();
            SentenceData = new Queue<string>();
            TextData = new Queue<string>();

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Buttons = GetButtons();
            nui = new Runtime();
            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            nui.NuiCamera.ElevationAngle = 16;

            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

            nui.SkeletonEngine.TransformSmooth = true;
            TransformSmoothParameters parameters = new TransformSmoothParameters();
            parameters.Smoothing = 0.5f;
            parameters.Correction = 0.0f;
            parameters.Prediction = 0.0f;
            parameters.JitterRadius = 1.0f;
            parameters.MaxDeviationRadius = 0.5f;
            nui.SkeletonEngine.SmoothParameters = parameters;
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Debug.Print("Found Person");
            SkeletonFrame allSkeletons = e.SkeletonFrame;
            TrackedSkeleton = (from s in allSkeletons.Skeletons
                               where s.TrackingState == SkeletonTrackingState.Tracked
                               select s).FirstOrDefault();
            if (TrackedSkeleton != null)
            {
                RecieveAndSetSkeletons(allSkeletons);

            }
        }

        void RecieveAndSetSkeletons(SkeletonFrame AllSkeletons)
        {
            TrackedSkeleton = (from s in AllSkeletons.Skeletons
                               where s.TrackingState == SkeletonTrackingState.Tracked
                               select s).FirstOrDefault();
            RunWordSearch();
        }

        public void RunWordSearch()
        {
            if (TrackedSkeleton != null)
            {
                Joint MotionHandJoint = (TrackedSkeleton.Joints[MotionHand]).ScaleTo(222, 1044);
                Joint MotionHandJointScaled = TrackedSkeleton.Joints[MotionHand].ScaleTo(1366, 768, 0.55f, 0.55f);

                Joint SelectionHandJoint = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(222, 656);
                Joint SelectionHandJointScaled = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(1366, 768, 0.55f, 0.55f);
                Point MotionHandPosition = new Point((MotionHandJointScaled.Position.X), (MotionHandJointScaled.Position.Y));
                Point SelectionHandPosition = new Point((SelectionHandJointScaled.Position.X), (SelectionHandJointScaled.Position.Y));

                //Move Cursor
                //SetCursorPos((int)(MotionHandPosition.X), (int)(MotionHandPosition.Y));
                Point destination_top_left = theCanvas.PointFromScreen(MotionHandPosition);
                destination_top_left = Point.Add(destination_top_left, new System.Windows.Vector(Pointer_Ellipse.Width / -2, Pointer_Ellipse.Height / -2));
                Canvas.SetTop(Pointer_Ellipse, destination_top_left.Y);
                Canvas.SetLeft(Pointer_Ellipse, destination_top_left.X);

                //Added data position to a queue of positions that will be loaded for each letter
                PositionData.Enqueue("\r\n\t\t\t\t<entry motionhand_x=\"" + MotionHandPosition.X +
                    "\" motionhand_y=\"" + MotionHandPosition.Y +
                    "\" selectionhand_x=\"" + SelectionHandPosition.X +
                    "\" selectionhand_y=\"" + SelectionHandPosition.Y +
                    "\" relative_timestamp=\"" + DateTime.Now.Subtract(StartTime).TotalMilliseconds +
                    "\" />");

                Gesture SelectionHandGesture = GestureTracker.track(TrackedSkeleton, TrackedSkeleton.Joints[SelectionHand], nui.NuiCamera.ElevationAngle);

                if (SelectionHandGesture != null && (SelectionHandGesture.id == GestureID.Push))
                {
                    foreach (System.Windows.Controls.Button beta in Buttons)
                    {
                        if (PointerOver(beta))
                        {
                            if ((((string)(beta.Content)).Equals(last_char) && DateTime.Now.Subtract(last_char_time).TotalSeconds > 0.75) || (!((string)(beta.Content)).Equals(last_char)))
                            {
                                SendKeys.SendWait(beta.Content.ToString().ToLowerInvariant());
                                CurrentWord += (beta.Content.ToString().ToLowerInvariant());
                                string letter = ("\r\n\t\t\t<print char=\"" + (beta.Content.ToString().ToLowerInvariant()) + "\" selection_hand_distance=\"" + SelectionHandDistance + "\" motion_hand_distance=\"" + MotionHandDistance +
                                    "\"");
                                while (PositionData.Count > 0)
                                {
                                    letter += PositionData.Dequeue();
                                }
                                PositionData = new Queue<string>();
                                letter += ("\r\n\t\t\t</print>");
                                WordData.Enqueue(letter);
                                SelectionHandDistance = 0.0;
                                MotionHandDistance = 0.0;

                                last_char = (string)(beta.Content);
                                last_char_time = DateTime.Now;
                            }
                        }
                    }
                    if (PointerOver(Button_Space))
                    {
                        if ((((string)(Button_Space.Content)).Equals(last_char) && DateTime.Now.Subtract(last_char_time).TotalSeconds > 0.75) || (!((string)(Button_Space.Content)).Equals(last_char)))
                        {
                            SendKeys.SendWait(" ");
                            string word = "\r\n\t\t<word text=\"" + CurrentWord + "\">";
                            CurrentWord = "";
                            while (WordData.Count > 0)
                            {
                                word += WordData.Dequeue();
                            }
                            word += "\r\n\t\t</word>";
                            SentenceData.Enqueue(word);
                            WordData = new Queue<string>();
                            last_char = (string)(Button_Space.Content);
                            last_char_time = DateTime.Now;
                        }
                    }
                    else if (PointerOver(Button_Backspace))
                    {
                        if ((last_char.Equals("bksp") && DateTime.Now.Subtract(last_char_time).TotalSeconds > 0.75) || (!last_char.Equals("bksp")))
                        {
                            SendKeys.SendWait("{Backspace}");
                            PositionData.Enqueue("\r\n\t\t\t\t<backspace motionhand_x=\"" + MotionHandPosition.X +
                                "\" motionhand_y=\"" + MotionHandPosition.Y +
                                "\" selectionhand_x=\"" + SelectionHandPosition.X +
                                "\" selectionhand_y=\"" + SelectionHandPosition.Y +
                                "\" relative_timestamp=\"" + DateTime.Now.Subtract(StartTime).TotalMilliseconds +
                                "\" />");
                            last_char = "bksp";
                            last_char_time = DateTime.Now;
                        }
                    }
                }

            }
        }

        private bool PointerOver(System.Windows.Controls.Button beta)
        {
            Point center_of_pointer = new Point(Canvas.GetLeft(Pointer_Ellipse) + Pointer_Ellipse.Width / 2,
                Canvas.GetTop(Pointer_Ellipse) + Pointer_Ellipse.Height / 2);
            Point top_left_element = new Point(Canvas.GetLeft(beta), Canvas.GetTop(beta));
            return ((center_of_pointer.X - top_left_element.X) <= beta.ActualWidth && (center_of_pointer.Y - top_left_element.Y) <= beta.ActualHeight
                && (center_of_pointer.X - top_left_element.X) >= 0 && (center_of_pointer.Y - top_left_element.Y) >= 0);
        }

        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            Byte[] ColoredBytes = GenerateColoredBytes(e.ImageFrame, currentVideoFrame);

            PlanarImage image = e.ImageFrame.Image;
            Depth_Image.Source = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null,
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

        private void Write(String s, FileStream f)
        {
            List<byte> bytes = new List<byte>();
            foreach (byte b in s)
            {
                bytes.Add(b);
            }
            f.Write(bytes.ToArray(), 0, bytes.Count);
        }

        private void Publish_Data_Click(object sender, RoutedEventArgs e)
        {
            using (FileStream DistanceFileWriter = System.IO.File.OpenWrite(StartTime.Month + "-" + StartTime.Day + "-" + StartTime.Year + " " + StartTime.Hour + "-" + StartTime.Minute + " movementtrack.xml"))
            {
                Write(("<movement date=\"" + StartTime.Month + "-" + StartTime.Day + "-" + StartTime.Year + " " + StartTime.Hour + "-" + StartTime.Minute + "\">"), DistanceFileWriter);
                while (TextData.Count > 0)
                {
                    Write(TextData.Dequeue(), DistanceFileWriter);
                }
                while (SentenceData.Count > 0)
                {
                    Write(SentenceData.Dequeue(), DistanceFileWriter);
                }
                while (WordData.Count > 0)
                {
                    Write(WordData.Dequeue(), DistanceFileWriter);
                }
                while (PositionData.Count > 0)
                {
                    Write(PositionData.Dequeue(), DistanceFileWriter);
                }
                Write("\r\n</movement>", DistanceFileWriter);
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void SwitchHandsButton_Click(object sender, RoutedEventArgs e)
        {
            JointID temporary = MotionHand;
            MotionHand = SelectionHand;
            SelectionHand = temporary;
            if (MotionHand == JointID.HandRight)
            {
                SwitchHandsButton.Content = "Switch Hand to Left";
            }
            else
            {
                SwitchHandsButton.Content = "Switch Hand to Right";
            }
        }

        private void Enter_Button_Click(object sender, RoutedEventArgs e)
        {
            SendKeys.SendWait("{Enter}");

            string sentence = "\r\n\t<sentence>";
            while (SentenceData.Count > 0)
            {
                sentence += SentenceData.Dequeue();
            }
            sentence += "\r\n\t\t</sentence>";
            TextData.Enqueue(sentence);
            SentenceData = new Queue<string>();
        }

        private List<System.Windows.Controls.Button> GetButtons()
        {
            List<System.Windows.Controls.Button> list = new List<System.Windows.Controls.Button>();
            list.Add(Button_A);
            list.Add(Button_B);
            list.Add(Button_C);
            list.Add(Button_D);
            list.Add(Button_E);
            list.Add(Button_F);
            list.Add(Button_G);
            list.Add(Button_H);
            list.Add(Button_I);
            list.Add(Button_J);
            list.Add(Button_K);
            list.Add(Button_L);
            list.Add(Button_M);
            list.Add(Button_N);
            list.Add(Button_O);
            list.Add(Button_P);
            list.Add(Button_Q);
            list.Add(Button_R);
            list.Add(Button_S);
            list.Add(Button_T);
            list.Add(Button_U);
            list.Add(Button_V);
            list.Add(Button_W);
            list.Add(Button_X);
            list.Add(Button_Y);
            list.Add(Button_Z);
            return list;
        }

    }
}
