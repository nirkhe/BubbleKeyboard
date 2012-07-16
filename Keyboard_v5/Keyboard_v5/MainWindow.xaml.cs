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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Keyboard : Window
    {
        SkeletonData TrackedSkeleton;

        Runtime nui;
        ImageFrame currentVideoFrame = new ImageFrame();

        Trigram Trigram;
        WordTreeNode InitialNode = new WordTreeNode(' ', true), CurrentNode;

        List<Bubble> Letters;

        KeyboardGestureTracker keyboardGuestureTracker = new KeyboardGestureTracker(15, 0.05f, 0.05f, 0.2f);
        GestureTracker regularGuestureTracker = new GestureTracker(15, 0.05f, 0.05f, 0.2f);

        JointID SelectionHand = JointID.HandRight;
        JointID MotionHand = JointID.HandLeft;

        FileStream DistanceFileWriter;
        double SelectionHandDistance = 0.0, MotionHandDistance = 0.0;

        Point MotionHandLast, SelectionHandLast;

        int[] RADIUS = { 150, 250 };
        int[] BUBBLERADIUS = { 48, 24 };

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        bool ReturnedToCenter = false;

        DateTime StartTime;
        Queue<string> PositionData;

        List<WordTreeNode> PreviousCharacterLocation;

        Stack<string> WordStack;

        public Keyboard()
        {
            loadDictionary();
            loadTrigram();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nui = new Runtime();
            nui.Initialize(RuntimeOptions.UseColor | RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_VideoFrameReady);
            nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            nui.NuiCamera.ElevationAngle = 1;

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

        private void loadDictionary()
        {

            using (System.IO.StreamReader sr = System.IO.File.OpenText("wordlist.txt"))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    int count = 0;
                    WordTreeNode currentNode = InitialNode;
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

        private void loadTrigram()
        {
            Trigram = new Trigram();
            using (System.IO.StreamReader sr = System.IO.File.OpenText("News.vfreq"))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    s = s.ToLowerInvariant();
                    string[] split = s.Split(' ');
                    if (split.Length == 2)
                    {
                        TrigramMainNode t = new TrigramMainNode(split[0], long.Parse(split[1]));
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
                 //Setup the default colors of the bubbles
                foreach (Bubble beta in Letters)
                {
                    if (beta.Ellipse.IsMouseOver)
                    {
                        beta.SetColor(Brushes.LawnGreen);
                    }
                    else
                    {
                        beta.SetColor(Brushes.LightYellow);
                    }
                }
                Joint MotionHandJoint = (TrackedSkeleton.Joints[MotionHand]).ScaleTo(222, 1044);
                Joint MotionHandJointScaled = TrackedSkeleton.Joints[MotionHand].ScaleTo(1366, 768, 0.55f, 0.55f);

                Joint SelectionHandJoint = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(222, 656);
                Joint SelectionHandJointScaled = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(1366, 768, 0.55f, 0.55f);
                Point MotionHandPosition = new Point((MotionHandJointScaled.Position.X), (MotionHandJointScaled.Position.Y));
                Point SelectionHandPosition = new Point((SelectionHandJointScaled.Position.X), (SelectionHandJointScaled.Position.Y));

                //Move Cursor
                SetCursorPos((int)(MotionHandPosition.X), (int)(MotionHandPosition.Y));

                //Added data position to a 
                PositionData.Enqueue("<entry motionhand_x=\"" + MotionHandPosition.X +
                    "\" motionhand_y=\"" + MotionHandPosition.Y +
                    "\" selectionhand_x=\"" + SelectionHandPosition.X +
                    "\" selectionhand_y=\"" + SelectionHandPosition.Y +
                    "\" timestamp=\"" + DateTime.Now.ToString() +
                    "\" relative_timestamp=\"" + DateTime.Now.Subtract(StartTime).TotalMilliseconds +
                    "\" />");

                if (SelectionHandLast == null)
                {
                    SelectionHandLast = SelectionHandPosition;
                }
                else
                {
                    SelectionHandDistance += Point.Subtract(SelectionHandLast, SelectionHandPosition).Length;
                    SelectionHandLast = SelectionHandPosition;
                }
                if (MotionHandLast == null)
                {
                    MotionHandLast = MotionHandPosition;
                }
                else
                {
                    MotionHandDistance += Point.Subtract(MotionHandLast, MotionHandPosition).Length;
                    MotionHandLast = MotionHandPosition;
                }

                Gesture MotionHandGuesture = keyboardGuestureTracker.track(TrackedSkeleton, TrackedSkeleton.Joints[MotionHand], nui.NuiCamera.ElevationAngle);
                Gesture SelectionHandGuesture = regularGuestureTracker.track(TrackedSkeleton, TrackedSkeleton.Joints[SelectionHand], nui.NuiCamera.ElevationAngle);

                if (CenterBubble_Ellipse.IsMouseOver)
                {
                    ReturnedToCenter = true;

                    if (SelectionHandGuesture != null && SelectionHandGuesture.id == GestureID.SwipeLeft)
                    {
                        SendKeys.SendWait("{Backspace}");
                        if (CenterBubble_Label.Content.ToString().Length > 0)
                        {
                            CenterBubble_Label.Content = CenterBubble_Label.Content.ToString().Substring(0, CenterBubble_Label.Content.ToString().Length - 1);
                            CurrentNode = (CurrentNode.parent != null ? CurrentNode.parent : CurrentNode);
                        }
                    }
                    else if (SelectionHandGuesture != null && SelectionHandGuesture.id == GestureID.Push)
                    {
                        RemoveLayout();
                        CurrentNode = InitialNode;
                        ConstructLetterLayout(Brushes.LightYellow);
                        SendKeys.SendWait(" ");
                        WordStack.Push(CenterBubble_Label.Content.ToString());
                        CenterBubble_Label.Content = "";
                    }

                }
                // We can make changes to the layout
                if (ReturnedToCenter)
                {
                    if (Enter_Button.IsMouseOver)
                    {
                        SendKeys.SendWait("{Enter}");
                        WordStack = new Stack<string>();
                        CenterBubble_Label.Content = "";
                        ReturnedToCenter = false;
                    }

                    if (SwitchHandsButton.IsMouseOver)
                    {
                        JointID temporary = MotionHand;
                        MotionHand = SelectionHand;
                        SelectionHand = temporary;
                        ReturnedToCenter = false;
                    }

                    if (MotionHandGuesture.id == GestureID.Still)
                    {
                        Bubble selected = null;
                        foreach (Bubble beta in Letters)
                        {
                            selected = beta.Ellipse.IsMouseOver ? beta : selected;
                            beta.SetColor(Brushes.LightYellow);
                        }
                        if (selected != null)
                        {
                            ReturnedToCenter = false;
                            char c = selected.GetCharacter();
                            RemoveLayout();
                            WordTreeNode NextNode = CurrentNode.HasChild(c);
                            if (NextNode == null)
                            {
                                NextNode = new WordTreeNode(c, false);
                                NextNode.parent = CurrentNode;
                            }
                            CurrentNode = NextNode;
                            ConstructLetterLayout(Brushes.LightYellow);
                            SendKeys.SendWait(c.ToString().ToLowerInvariant());
                            CenterBubble_Label.Content = CenterBubble_Label.Content.ToString() + c.ToString();

                            Write("\t<print char=\"" + c + "\">");
                            while (PositionData.Count > 0)
                            {
                                Write("\t\t" + PositionData.Dequeue());
                            }
                            PositionData = new Queue<string>();
                            Write("\t</print>");

                            SelectionHandDistance = 0.0;
                            MotionHandDistance = 0.0;

                            if (CurrentNode.HasChild('!') != null)
                            {
                                CenterBubble_Ellipse.Fill = Brushes.AntiqueWhite;
                            }
                            else
                            {
                                CenterBubble_Ellipse.Fill = Brushes.GreenYellow;
                            }
                        }
                    }
                }
            }
        }

        private void ConstructLetterLayout(Brush Color)
        {
            Letters = new List<Bubble>();

            //START TRIGRAM SORTING ----------------------------------------------------

            List<TrigramTrinaryNode> TrigramList = null;
            if (WordStack.Count >= 2)
            {
                string last = WordStack.Pop();
                string twoback = WordStack.Peek();
                WordStack.Push(last);
                TrigramList = Trigram.GetList(twoback, last);
            }

            if (TrigramList == null)
            {
                TrigramList = new List<TrigramTrinaryNode>();
            }

            string WordSoFar = (string)(CenterBubble_Label.Content);

            List<WordTreeNode> TrigramChars = new List<WordTreeNode>();

            foreach (TrigramTrinaryNode ttn in TrigramList)
            {
                string word = Trigram.MainNodes[ttn.WordIndex].text;
                if (word.StartsWith(WordSoFar, true, null))
                {
                    char next = ' ';
                    if (word.Length > WordSoFar.Length)
                    {
                        next = word[WordSoFar.Length];
                    }
                    bool found = false;
                    foreach (WordTreeNode wtn in TrigramChars)
                    {
                        if (wtn.character == next)
                        {
                            wtn.count += (int)(ttn.frequency);
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        WordTreeNode newWtn = new WordTreeNode(next, false);
                        newWtn.count = (int)(ttn.frequency);
                        TrigramChars.Add(newWtn);
                    }
                }
            }

            //END TRIGRAM SORTING -----------------------------------------------------

            List<WordTreeNode> finalChars = new List<WordTreeNode>();

            while (finalChars.Count < Math.Min(9, TrigramChars.Count) && TrigramChars.Count > 0)
            {
                WordTreeNode max = TrigramChars[0];
                foreach (WordTreeNode t in TrigramChars)
                {
                    if (t.count > max.count)
                        max = t;
                }
                finalChars.Add(max);
                TrigramChars.Remove(max);
            }

            int count = 0;

            CurrentNode.children = Organize(CurrentNode.children);

            while (finalChars.Count < 9 && count < CurrentNode.children.Count)
            {
                char n = CurrentNode.children[count].character;
                if (n != '!')
                {
                    bool alreadyExists = false;
                    foreach (WordTreeNode wtn in finalChars)
                    {
                        if (n == wtn.character)
                        {
                            alreadyExists = true;
                        }
                    }
                    if (!alreadyExists)
                    {
                        finalChars.Add(CurrentNode.children[count]);
                    }
                }
                count++;
            }
            count = 0;
            while (finalChars.Count < 9)
            {
                char n = InitialNode.children[count].character;
                bool alreadyExists = false;
                foreach (WordTreeNode wtn in finalChars)
                {
                    if (n == wtn.character)
                    {
                        alreadyExists = true;
                    }
                }
                if (!alreadyExists)
                {
                    finalChars.Add(new WordTreeNode(n, false));
                }
                count++;
            }

            WordTreeNode[] placedChars = new WordTreeNode[9];
            List<WordTreeNode> placeLater = new List<WordTreeNode>();

            for (int i = 0; i < 9; i++)
            {
                WordTreeNode wtn = finalChars[i];
                int place = -1;
                foreach (WordTreeNode a in PreviousCharacterLocation)
                {
                    if (wtn.character == a.character)
                    {
                        place = a.count;
                    }
                }
                if (place == -1)
                {
                    placeLater.Add(wtn);
                }
                else
                {
                    int ct = 0;
                    while (true)
                    {
                        int position = (((int)(Math.Pow((-1), (ct + 1)))) * ((ct + 1) / 2) + place) % 9;
                        while (position < 0)
                        {
                            position += 9;
                        }
                        ct++;
                        if (placedChars[position] == null)
                        {
                            placedChars[position] = wtn;
                            break;
                        }
                    }
                }
            }

            placeLater.Sort();

            foreach (WordTreeNode wtn in placeLater)
            {
                bool placed = false;
                int ct = 0;
                while (!placed)
                {
                    if (placedChars[ct] == null)
                    {
                        placedChars[ct] = wtn;
                        placed = true;
                    }
                    ct++;
                }
            }

            //Now placedChars contains all the characters in the order they need to be placed -----------

            for (int i = 0; i < 9; i++)
            {
                WordTreeNode wtn = placedChars[i];
                WordTreeNode listing = null;
                foreach (WordTreeNode position in PreviousCharacterLocation)
                {
                    if (position.character == wtn.character)
                    {
                        listing = position;
                        listing.count = i;
                    }
                }
                if (listing != null)
                {
                    PreviousCharacterLocation.Remove(listing);
                    PreviousCharacterLocation.Insert(0, listing);
                }
                else
                {
                    WordTreeNode newwtn = new WordTreeNode(wtn.character, false);
                    newwtn.count = i;
                    PreviousCharacterLocation.Insert(0, newwtn);
                }
            }

            double cutTheta = 2 * Math.PI / 9;
            for (int i = 0; i < 9; i++)
            {
                WordTreeNode wtn = placedChars[i];

                Letters.Add(new Bubble(theCanvas,
                    new Point(theCanvas.Width / 2 + RADIUS[0] * Math.Sin(cutTheta * i), theCanvas.Height / 2 + RADIUS[0] * -Math.Cos(cutTheta * i)),
                    BUBBLERADIUS[0], Color, wtn.character));
            }

            cutTheta = 2 * Math.PI / 27;
            for (int i = 0; i < 26; i++)
            {
                Letters.Add(new Bubble(theCanvas, new Point(theCanvas.Width / 2 + RADIUS[1] * Math.Sin(cutTheta * i), theCanvas.Height / 2 + RADIUS[1] * -Math.Cos(cutTheta * i)),
                    BUBBLERADIUS[1], Color, (char)((int)('A') + i)));
            }

            Letters.Add(new Bubble(theCanvas, new Point(theCanvas.Width / 2 + RADIUS[1] * Math.Sin(26 * cutTheta), theCanvas.Height / 2 + RADIUS[1] * -Math.Cos(26 * -cutTheta)),
                BUBBLERADIUS[1], Color, '-'));

        }

        private void RemoveLayout()
        {
            foreach (Bubble beta in Letters)
            {
                beta.RemoveFromParent();
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

        private List<WordTreeNode> Organize(List<WordTreeNode> list)
        {
            if (list.Count <= 1)
            {
                return list;
            }
            List<WordTreeNode> left, right;
            int middle = list.Count / 2;
            left = SubList(list, 0, middle);
            right = SubList(list, middle, list.Count);
            left = Organize(left);
            right = Organize(right);

            return Merge(left, right);
        }

        private List<WordTreeNode> SubList(List<WordTreeNode> list, int start, int end)
        {
            List<WordTreeNode> toReturn = new List<WordTreeNode>();
            for (int i = start; i < end; i++)
            {
                toReturn.Add(list[i]);
            }
            return toReturn;
        }

        private List<WordTreeNode> Merge(List<WordTreeNode> alpha, List<WordTreeNode> beta)
        {
            List<WordTreeNode> result = new List<WordTreeNode>();

            while (alpha.Count > 0 || beta.Count > 0)
            {
                if (alpha.Count > 0 && beta.Count > 0)
                {
                    if (alpha[0].count > beta[0].count)
                    {
                        result.Add(alpha[0]);
                        alpha.RemoveAt(0);
                    }
                    else
                    {
                        result.Add(beta[0]);
                        beta.RemoveAt(0);
                    }
                }
                else if (alpha.Count > 0)
                {
                    result.Add(alpha[0]);
                    alpha.RemoveAt(0);
                }
                else if (beta.Count > 0)
                {
                    result.Add(beta[0]);
                    beta.RemoveAt(0);
                }
            }

            return result;
        }

        private FileStream ConstructFileStream(string activeDirectory, string subfolderName, string fileName)
        {
            string newPath = System.IO.Path.Combine(activeDirectory, subfolderName);
            System.IO.Directory.CreateDirectory(newPath);
            newPath = System.IO.Path.Combine(activeDirectory, fileName);
            return System.IO.File.Create(newPath);
        }

        private void Write(String s)
        {
            List<byte> bytes = new List<byte>();
            foreach (byte b in s)
            {
                bytes.Add(b);
            }
            DistanceFileWriter.Write(bytes.ToArray(), 0, bytes.Count);
        }

    }
}
