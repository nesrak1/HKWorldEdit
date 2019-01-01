using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Node
{
    private bool selected;
    public bool Selected
    {
        get
        {
            return selected;
        }
        set
        {
            selected = value;
            if (selected)
            {
                elem.SetSelectedColor(new Color(1, 0, 0));
            }
            else
            {
                elem.SetSelectedColor(new Color(0, 0, 0));
            }
        }
    }

    public string text;
    public Rect shape;
    public FsmTransition[] transitions;
    public HandleElement elem;
    public Node(string text, Rect shape, FsmTransition[] transitions, HandleElement elem)
    {
        this.text = text;
        this.shape = shape;
        this.transitions = transitions;
        this.elem = elem;

        selected = false;
    }
}