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
            //for (int i = 0; i < MainNodes.Count; i++)
            //{
            //    if (MainNodes[i].text.Equals(x))
            //        return i;
            //}
            //return -1;
            return Search(x, 0, MainNodes.Count - 1);
        }

        public int Search(string x, int StartIndex, int EndIndex)
        {
            if (EndIndex < StartIndex)
            {
                return -1;
            }
            else
            {
                int mid = StartIndex + ((EndIndex - StartIndex) / 2);

                int comparison = MainNodes[mid].CompareTo(x);

                if (comparison == -1)
                {
                    return Search(x, StartIndex, mid - 1);
                }
                else if (comparison == 1)
                {
                    return Search(x, mid + 1, EndIndex);
                }
                else
                {
                    return mid;
                }
            }
        }
    }

    class TrigramMainNode: IComparable
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
            //for (int i = 0; i < SecondaryNodeChildren.Count; i++)
            //{
            //    if (SecondaryNodeChildren[i].index == index)
            //        return i;
            //}
            //return -1;
            return Search(index, 0, SecondaryNodeChildren.Count);
        }

        public int Search(int index, int StartIndex, int EndIndex)
        {
            if (EndIndex < StartIndex)
            {
                return -1;
            }
            else
            {
                int mid = StartIndex + ((EndIndex - StartIndex) / 2);

                int comparison = SecondaryNodeChildren[mid].index.CompareTo(index);

                if (comparison == -1)
                {
                    return Search(index, StartIndex, mid - 1);
                }
                else if (comparison == 1)
                {
                    return Search(index, mid + 1, EndIndex);
                }
                else
                {
                    return mid;
                }
            }
            
        }

        public int CompareTo(Object arg0)
        {
            if (arg0.GetType() == this.GetType())
            {
                TrigramMainNode other = (TrigramMainNode)(arg0);
                return this.text.CompareTo(other.text);
            }
            return 1;
        }
    }

    class TrigramSecondaryNode : IComparable
    {
        public int index;
        public List<TrigramTrinaryNode> TrinaryNodeChildren;

        public TrigramSecondaryNode(int index)
        {
            TrinaryNodeChildren = new List<TrigramTrinaryNode>();
            this.index = index;
        }

        public int CompareTo(Object arg0)
        {
            if (arg0.GetType() == this.GetType())
            {
                TrigramSecondaryNode other = (TrigramSecondaryNode)(arg0);
                return this.index.CompareTo(other.index);
            }
            return 1;
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
                if (st.character == c.ToString().ToLowerInvariant()[0] || st.character == c.ToString().ToUpperInvariant()[0])
                {
                    return st;
                }
            }
            return null;
        }

        public override bool Equals(Object arg0)
        {
            if (arg0.GetType() == this.GetType())
            {
                return this.character.Equals(((WordTreeNode)(arg0)).character);
            }
            return false;
        }
    }
}
