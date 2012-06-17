using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class TrigramMainNode
{
    public string text;
    public long frequency;
    public List<TrigramSecondaryNode> SecondaryNodeChildren;

    public TrigramMainNode(string text, long frequency)
    {
        this.text = text;
        this.frequency = frequency;
        this.SecondaryNodeChildren = new List<TrigramSecondaryNode> ();
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
