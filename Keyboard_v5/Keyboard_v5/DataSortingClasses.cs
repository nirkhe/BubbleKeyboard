using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keyboard_v5
{
    class Trigram
    {
        public List<TrigramMainNode> MainNodes;

        public Trigram()
        {
            MainNodes = new List<TrigramMainNode>();
            MainNodes.Add(new TrigramMainNode(null, -1));
        }


        public List<TrigramTrinaryNode> GetList(string first, string second)
        {
            int FirstIndex = Search(first);
            if (FirstIndex >= 0)
            {
                TrigramMainNode MainNode = MainNodes[FirstIndex];
                int SecondIndex = Search(second);
                if (SecondIndex >= 0)
                {
                    List<TrigramTrinaryNode> ToReturn = MainNode.GetList(SecondIndex);
                    if (ToReturn == null)
                    {
                        return null;
                    }
                    else
                    {
                        return ToReturn;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public int Search(string x)
        {
            for (int i = 0; i < MainNodes.Count; i++)
            {
                if (MainNodes[i].text == x)
                    return i;
            }
            return -1;
        }

        public int Search(string x, int StartIndex, int EndIndex)
        {
            int midpoint = (StartIndex + EndIndex) / 2;
            int value = MainNodes[midpoint].text.CompareTo(x);

            if (StartIndex == EndIndex)
            {
                return -1;
            }
            if (value == 1)
            {
                return Search(x, StartIndex, midpoint - 1);
            }
            else if (value == 0)
            {
                return midpoint;
            }
            else if (value == -1)
            {
                return Search(x, midpoint + 1, EndIndex);
            }
            else
            {
                return -1;
            }
        }
    }

    class TrigramMainNode
    {
        public string text;
        public long frequency;
        public List<TrigramSecondaryNode> SecondaryNodeChildren;

        public TrigramMainNode(string text, long frequency)
        {
            this.text = text;
            this.frequency = frequency;
            this.SecondaryNodeChildren = new List<TrigramSecondaryNode>();
        }

        public TrigramMainNode(string text, long frequency, List<TrigramSecondaryNode> SecondaryNodeChildren)
        {
            this.text = text;
            this.frequency = frequency;
            this.SecondaryNodeChildren = SecondaryNodeChildren;
        }

        public List<TrigramTrinaryNode> GetList(int index)
        {
            int ListPoint = Search(index);
            if (ListPoint >= 0)
            {
                return SecondaryNodeChildren[ListPoint].TrinaryNodeChildren;
            }
            else
            {
                return null;
            }
        }

        public int Search(int index)
        {
            for (int i = 0; i < SecondaryNodeChildren.Count; i++)
            {
                if (SecondaryNodeChildren[i].index == index)
                    return i;
            }
            return -1;
        }

        public int Search(int index, int StartIndex, int EndIndex)
        {
            int midpoint = (StartIndex + EndIndex) / 2;
            if (StartIndex == EndIndex)
            {
                return -1;
            }

            int value = SecondaryNodeChildren[midpoint].index;

            if (index < value)
            {
                return Search(index, StartIndex, midpoint - 1);
            }
            else if (index == value)
            {
                return midpoint;
            }
            else if (index > value)
            {
                return Search(index, midpoint + 1, EndIndex);
            }
            else
            {
                return -1;
            }
        }
    }

    class TrigramSecondaryNode
    {
        public int index;
        public List<TrigramTrinaryNode> TrinaryNodeChildren;

        public TrigramSecondaryNode(int index)
        {
            TrinaryNodeChildren = new List<TrigramTrinaryNode>();
            this.index = index;
        }
    }

    class TrigramTrinaryNode : IComparable
    {
        public int WordIndex;
        public long frequency;

        public TrigramTrinaryNode(int WordIndex, long frequency)
        {
            this.WordIndex = WordIndex;
            this.frequency = frequency;
        }

        public int CompareTo(Object obj)
        {
            if (obj.GetType().Equals(this.GetType()))
            {
                TrigramTrinaryNode other = (TrigramTrinaryNode)obj;
                if (this.frequency < other.frequency)
                {
                    return -1;
                }
                else if (this.frequency == other.frequency)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }


            }
            else
            {
                return 1;
            }
        }
    }

    class WordTreeNode : IComparable
    {
        public WordTreeNode(char character, bool isInitialNode)
        {
            this.character = character;
            children = new List<WordTreeNode>();
            this.isInitialNode = isInitialNode;
            count = 0;
        }

        public int CompareTo(Object obj)
        {

            if (obj.GetType().Equals(this.GetType()))
            {
                WordTreeNode other = (WordTreeNode)(obj);

                return this.character.CompareTo(other.character);
            }
            else
            {
                return 1;
            }
        }

        public char character;
        public WordTreeNode parent;
        public List<WordTreeNode> children;
        public int count;
        public bool isInitialNode;

        public void AddChild(WordTreeNode st)
        {
            children.Add(st);
            st.SetParent(this);
        }

        public void SetParent(WordTreeNode st)
        {
            parent = st;
            if (character == '!')
            {
                count++;
                parent.AddCount();
            }
        }

        public void AddCount()
        {
            count++;
            if (!isInitialNode)
            {
                parent.AddCount();
            }
        }

        /**
         * Sees if there exists a child of the character
         * If exists then returns child
         * Else returns null
         **/
        public WordTreeNode HasChild(char c)
        {
            foreach (WordTreeNode st in children)
            {
                if (st.character == c)
                {
                    return st;
                }
            }
            return null;
        }

    }
}
