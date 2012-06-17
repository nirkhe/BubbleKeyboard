using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class TrigramSecondaryNode
{
    public int index;
    public List<TrigramTrinaryNode> TrinaryNodeChildren;

    public TrigramSecondaryNode(int index)
    {
        TrinaryNodeChildren = new List<TrigramTrinaryNode>();
        this.index = index;
    }


}

