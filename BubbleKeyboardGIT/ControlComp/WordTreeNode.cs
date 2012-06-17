using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class WordTreeNode : IComparable
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
