using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



public class Trigram
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

