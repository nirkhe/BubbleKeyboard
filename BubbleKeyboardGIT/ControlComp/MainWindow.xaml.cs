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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MouseMonitor monitor = new MouseMonitor(0.70f);
        GuestureTracker tracker = new GuestureTracker(15, 0.05f, 0.05f, 0.2f);
        SkeletonData trackedSkeleton;

        Runtime nui;
        ImageFrame currentVideoFrame = new ImageFrame();
        //KinectKeyboard keyboard;
        BubbleKeyboard BubbleKeyboard;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nui = new Runtime();
            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            nui.NuiCamera.ElevationAngle = 25;

            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            //keyboard = new KinectKeyboard(nui.NuiCamera.ElevationAngle,initialNode);
            BubbleKeyboard = new BubbleKeyboard(nui.NuiCamera.ElevationAngle, initialNode, Trigram);

            nui.SkeletonEngine.TransformSmooth = true;
            TransformSmoothParameters parameters = new TransformSmoothParameters();
            parameters.Smoothing = 0.5f;
            parameters.Correction = 0.0f;
            parameters.Prediction = 0.0f;
            parameters.JitterRadius = 1.0f;
            parameters.MaxDeviationRadius = 0.5f;
            nui.SkeletonEngine.SmoothParameters = parameters;

        }

        WordTreeNode initialNode = new WordTreeNode(' ', true);

        public MainWindow()
        {
            loadDictionary();
            loadTrigram();
            InitializeComponent();

        }

        Trigram Trigram;

        private void loadTrigram()
        {
            Trigram = new Trigram();
            using (System.IO.StreamReader sr = System.IO.File.OpenText("News.vfreq"))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    s = s.ToUpperInvariant();
                    string[] split = s.Split(' ');
                    if (split.Length == 2)
                    {
                        TrigramMainNode t = new TrigramMainNode(split[0],long.Parse(split[1]));
                        Trigram.MainNodes.Add(t);
                    }
                }
            }

            using (System.IO.StreamReader sr = System.IO.File.OpenText("News.id3gram"))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    s = s.ToUpperInvariant();
                    string[] split = s.Split(' ');
                    int[] parse = new int[split.Length];
                    bool fit = true;
                    for (int i = 0; i < split.Length; i++)
                    {
                        parse[i] = int.Parse(split[i]);
                        if (parse[i] > Trigram.MainNodes.Count)
                            fit = false;
                    }
                    if (split.Length == 4 && fit)
                    {
                        TrigramMainNode tmn = Trigram.MainNodes[parse[0]];
                        TrigramMainNode secondary = Trigram.MainNodes[parse[1]];
                        TrigramMainNode trinary = Trigram.MainNodes[parse[2]];
                        TrigramSecondaryNode tsn = null;
                        if (tmn.SecondaryNodeChildren.Count > 0 && tmn.SecondaryNodeChildren[tmn.SecondaryNodeChildren.Count - 1].index == parse[1])
                        {
                            tsn = tmn.SecondaryNodeChildren[tmn.SecondaryNodeChildren.Count - 1];
                        }
                        if (tsn == null)
                        {
                            tsn = new TrigramSecondaryNode(parse[1]);
                            tmn.SecondaryNodeChildren.Add(tsn);
                        }
                        TrigramTrinaryNode ttn = new TrigramTrinaryNode(parse[2], (long)(parse[3]));
                        tsn.TrinaryNodeChildren.Add(ttn);
                    }
                }
            }
        }

        private void loadDictionary()
        {

            using (System.IO.StreamReader sr = System.IO.File.OpenText("wordlist.txt"))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    int count = 0;
                    WordTreeNode currentNode = initialNode;
                    while (count < s.Length)
                    {
                        WordTreeNode next = currentNode.HasChild(s[count]);
                        if (next == null)
                        {
                            next = new WordTreeNode(s[count], false);
                            currentNode.AddChild(next);
                        }
                        currentNode = next;
                        count++;
                    }
                    currentNode.AddChild(new WordTreeNode('!', false));
                }
            }
        }


        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            //Debug.Print("Found Person");
            SkeletonFrame allSkeletons = e.SkeletonFrame;
            trackedSkeleton = (from s in allSkeletons.Skeletons
                               where s.TrackingState == SkeletonTrackingState.Tracked
                               select s).FirstOrDefault();
            if (trackedSkeleton != null)
            {
                // keyboard.recieveAndSetSkeletons(allSkeletons);
                //BubbleKeyboard.InitializeComponent();
                BubbleKeyboard.RecieveAndSetSkeletons(allSkeletons);

            }
        }

        void nui_VideoFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            currentVideoFrame = e.ImageFrame;
            //image2.Source = e.ImageFrame.ToBitmapSource();
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


    }
}
