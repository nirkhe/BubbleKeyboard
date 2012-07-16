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

namespace Keyboard_v5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Keyboard : Window
    {
        SkeletonData trackedSkeleton;

        Runtime nui;
        ImageFrame currentVideoFrame = new ImageFrame();

        Trigram Trigram;


        public Keyboard()
        {
            loadDictionary();
            loadTrigram();
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
    }
}
