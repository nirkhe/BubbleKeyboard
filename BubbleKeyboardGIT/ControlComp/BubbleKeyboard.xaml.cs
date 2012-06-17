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

namespace ControlCompWithBubbleKeyboard
{
    /// <summary>
    /// Interaction logic for BubbleKeyboard.xaml
    /// </summary>
    public partial class BubbleKeyboard : Window
    {
        public WordTreeNode InitialNode, CurrentNode;
        public double theta;
        public int ElevationAngle;
        public Trigram Trigram;

        public SkeletonData TrackedSkeleton;

        public List<Bubble> Letters, Symbols;

        public List<WordTreeNode> PreviousCharacterLocation;

        private Stack<string> WordStack;

        private int[] RADIUS = { 150, 250 };
        private int[] BUBBLERADIUS = { 48, 24 };

        private KeyboardGuestureTracker keyboardGuestureTracker = new KeyboardGuestureTracker(15, 0.05f, 0.05f, 0.2f);
        private GuestureTracker regularGuestureTracker = new GuestureTracker(15, 0.05f, 0.05f, 0.2f);

        private JointID SelectionHand = JointID.HandRight;
        private JointID MotionHand = JointID.HandLeft;

        private FileStream DistanceFileWriter;
        private double SelectionHandDistance = 0.0, MotionHandDistance = 0.0;

        private Point MotionHandLast, SelectionHandLast;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        public BubbleKeyboard(int ElevationAngle, WordTreeNode InitialNode, Trigram Trigram)
        {
            this.ElevationAngle = ElevationAngle;
            this.theta = ElevationAngle * Math.PI / 180;
            this.InitialNode = InitialNode;
            InitializeComponent();
            this.CurrentNode = this.InitialNode;
            this.Trigram = Trigram;

            WordStack = new Stack<string>();

            PreviousCharacterLocation = new List<WordTreeNode>();
            ConstructLetterLayout(Brushes.LightYellow);

            DistanceFileWriter = ConstructFileStream("C:/users/chinmay/dropbox/kinect/BubbleKeyboardTakeFour/", "distanceFiles", DateTime.Now.ToString() + "_distancechars");

            this.Show();
        }

        private bool ReturnedToCenter = false;
        private bool LetterLayoutIsOn = true;

        public void RunWordSearch()
        {
            if (TrackedSkeleton != null)
            {
                if (LetterLayoutIsOn)
                {
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
                }
                else
                {
                    foreach (Bubble beta in Symbols)
                    {
                        if (beta.Ellipse.IsMouseOver)
                        {
                            beta.SetColor(Brushes.LawnGreen);
                        }
                        else
                        {
                            beta.SetColor(Brushes.LightPink);
                        }
                    }
                }
                

                Joint MotionHandJoint = (TrackedSkeleton.Joints[MotionHand]).ScaleTo(222, 1044);
                Joint MotionHandJointScaled = TrackedSkeleton.Joints[MotionHand].ScaleTo(1366, 768, 0.55f, 0.55f);

                Joint SelectionHandJoint = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(222, 656);
                Joint SelectionHandJointScaled = (TrackedSkeleton.Joints[SelectionHand]).ScaleTo(1366, 768, 0.55f, 0.55f);
                Point MotionHandPosition = new Point((MotionHandJointScaled.Position.X), (MotionHandJointScaled.Position.Y));
                Point SelectionHandPosition = new Point((SelectionHandJointScaled.Position.X), (SelectionHandJointScaled.Position.Y));

                SetCursorPos((int)(MotionHandPosition.X), (int)(MotionHandPosition.Y));

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
                    SelectionHandDistance += Point.Subtract(MotionHandLast, MotionHandPosition).Length;
                    MotionHandLast = MotionHandPosition;
                }

                Guesture MotionHandGuesture = keyboardGuestureTracker.track(TrackedSkeleton, TrackedSkeleton.Joints[MotionHand], ElevationAngle);
                Guesture SelectionHandGuesture = regularGuestureTracker.track(TrackedSkeleton, TrackedSkeleton.Joints[SelectionHand], ElevationAngle);

                if (CenterBubble_Ellipse.IsMouseOver)
                {
                    ReturnedToCenter = true;

                    if (SelectionHandGuesture != null && SelectionHandGuesture.id == GuestureID.SwipeLeft)
                    {
                        SendKeys.SendWait("{Backspace}");
                        if (CenterBubble_Label.Content.ToString().Length > 0)
                        {
                            CenterBubble_Label.Content = CenterBubble_Label.Content.ToString().Substring(0, CenterBubble_Label.Content.ToString().Length - 1);
                            CurrentNode = (CurrentNode.parent != null ? CurrentNode.parent : CurrentNode);
                        }
                    }
                    else if (SelectionHandGuesture != null && SelectionHandGuesture.id == GuestureID.SwipeUp && LetterLayoutIsOn)
                    {
                        RemoveLayout(Letters);
                        ConstructSymbolLayout(Brushes.LightPink);
                        LetterLayoutIsOn = false;
                    }
                    else if (SelectionHandGuesture != null && SelectionHandGuesture.id == GuestureID.SwipeDown && !LetterLayoutIsOn)
                    {
                        RemoveLayout(Symbols);
                        ConstructLetterLayout(Brushes.LightYellow);
                        LetterLayoutIsOn = true;
                    }
                    else if (SelectionHandGuesture != null && SelectionHandGuesture.id == GuestureID.Push)
                    {
                        RemoveLayout(LetterLayoutIsOn ? Letters : Symbols);
                        CurrentNode = InitialNode;
                        if (LetterLayoutIsOn)
                        {
                            ConstructLetterLayout(Brushes.LightYellow);
                        }
                        else
                        {
                            ConstructSymbolLayout(Brushes.LightPink);
                        }
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

                    if (MotionHandGuesture.id == GuestureID.Still)
                    {
                        if (LetterLayoutIsOn)
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
                                RemoveLayout(Letters);
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
                                string s = (DateTime.Now.ToString() + "&" + c.ToString() + "&" + SelectionHandDistance + "&" + MotionHandDistance);
                                List<byte> bytes = new List<byte>();
                                foreach (byte b in s)
                                {
                                    bytes.Add(b);
                                }
                                DistanceFileWriter.Write(bytes.ToArray(), 0, bytes.Count);
                                SelectionHandDistance = MotionHandDistance = 0.0;

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
                        else
                        {
                            Bubble selected = null;
                            foreach (Bubble beta in Symbols)
                            {
                                selected = beta.Ellipse.IsMouseOver ? beta : selected;
                                beta.SetColor(Brushes.LightPink);
                                if (selected != null)
                                    selected.SetColor(Brushes.LightSeaGreen);
                            }
                            if (selected != null)
                            {
                                ReturnedToCenter = false;
                                SendKeys.SendWait(selected.GetCharacter().ToString());
                                CenterBubble_Label.Content = CenterBubble_Label.Content.ToString() + selected.GetCharacter().ToString();
                            }
                        }
                    }
                }
            }
        }

        public void RecieveAndSetSkeletons(SkeletonFrame AllSkeletons)
        {
            this.TrackedSkeleton = (from s in AllSkeletons.Skeletons
                                    where s.TrackingState == SkeletonTrackingState.Tracked
                                    select s).FirstOrDefault();
            RunWordSearch();
        }

        private bool ContainsCharacter(List<WordTreeNode> list, char character)
        {
            bool contains = false;
            foreach (WordTreeNode wtn in list)
            {
                contains = contains || wtn.character == character;
            }
            return contains;
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

            List<WordTreeNode> TrigramChars = new List<WordTreeNode> ();

            foreach (TrigramTrinaryNode ttn in TrigramList)
            {
                string word = Trigram.MainNodes[ttn.WordIndex].text;
                if (word.StartsWith(WordSoFar,true,null))
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
                        int position = ( ( (int)(Math.Pow((-1),(ct + 1)) ) ) * ( (ct + 1) / 2 ) + place ) % 9;
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

        private void ConstructSymbolLayout(Brush Color)
        {
            Symbols = new List<Bubble>();

            double cutTheta = 2 * Math.PI / 10;

            for (int i = 0; i <= 9; i++)
            {
                Symbols.Add(new Bubble(theCanvas,
                    new Point(theCanvas.Width / 2 + RADIUS[0] * Math.Sin(cutTheta * i), theCanvas.Height / 2 + RADIUS[0] * -Math.Cos(cutTheta * i)),
                    BUBBLERADIUS[0], Color, (char)(i + '0')));
            }

            char[] Punctuation = { '.', ',', ')', '}', ']', '>', '/', ';', ',', '\"', '!', '&', '%', '$', '#', '@', '\\', '<', '[', '{', '(', '?' };

            cutTheta = 2 * Math.PI / Punctuation.Length;

            for (int i = 0; i < Punctuation.Length; i++)
            {
                Symbols.Add(new Bubble(theCanvas,
                    new Point(theCanvas.Width / 2 + RADIUS[1] * Math.Sin(cutTheta * i), theCanvas.Height / 2 + RADIUS[1] * -Math.Cos(cutTheta * i)),
                    BUBBLERADIUS[1], Color, Punctuation[i]));
            }

        }

        private void RemoveLayout(List<Bubble> Layout)
        {
            foreach (Bubble beta in Layout)
            {
                beta.RemoveFromParent();
            }
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

    }
}
