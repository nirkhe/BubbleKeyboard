using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class TrigramTrinaryNode : IComparable
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

