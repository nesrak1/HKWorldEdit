using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class HandleElement
{
    private HandleType type;
    private Vector2 start;
    private Vector2 end;
    private Vector2 offset;

    private Vector2 bez1;
    private Vector2 bez2;

    private Color color;
    private GUIStyle style;

    private Color selectedColor;

    private string text;

    public HandleElement(HandleType type, Vector2 start, Vector2 end) : base()
    {
        this.type = type;
        this.start = start;
        this.end = end;

        text = "";
        color = Color.black;
        selectedColor = Color.black;
    }
    public HandleElement(HandleType type, Vector2 start, Vector2 end, Color color) : base()
    {
        this.type = type;
        this.start = start;
        this.end = end;
        this.color = color;

        text = "";
        selectedColor = Color.black;
    }
    public HandleElement(HandleType type, Vector2 start, int width, string text) : base()
    {
        this.type = type;
        this.start = start;
        this.text = text;

        style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.fixedWidth = width;
        end = new Vector2(0, 0);
        color = Color.black;
        selectedColor = Color.black;
    }
    public HandleElement(HandleType type, Vector2 start, Vector2 end, Vector2 bez1, Vector2 bez2) : base()
    {
        this.type = type;
        this.start = start;
        this.end = end;
        this.bez1 = bez1;
        this.bez2 = bez2;

        style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.UpperCenter;
        style.fixedWidth = 10;
        color = Color.black;
        selectedColor = Color.black;
    }
    public HandleElement(HandleType type, Vector2 start, Vector2 end, Vector2 bez1, Vector2 bez2, Color color) : base()
    {
        this.type = type;
        this.start = start;
        this.end = end;
        this.bez1 = bez1;
        this.bez2 = bez2;
        this.color = color;
        selectedColor = Color.black;
    }
    
    public void SetPosition(Vector2 start)
    {
        Vector2 change = this.start - start;
        this.start = start;

        end += change;
    }

    public void SetPosition(Vector2 start, Vector2 end)
    {
        this.start = start;
        this.end = end;
    }

    public void Transform(Vector2 pos)
    {
        offset = pos;
    }

    public void Translate(Vector2 pos)
    {
        offset += pos;
    }

    public void SetSelectedColor(Color color)
    {
        selectedColor = color;
    }

    public void Draw()
    {
        switch (type) {
            case HandleType.Rectangle:
            {
                Handles.DrawSolidRectangleWithOutline(new Rect(start + offset, end - start), color, selectedColor);
                break;
            }
            case HandleType.Line:
            {
                Handles.DrawLine(start + offset, end + offset);
                break;
            }
            case HandleType.ArrowLine:
            {
                Handles.DrawBezier(start + offset, end + offset, bez1 + offset, bez2 + offset, color, null, 3);
                
                Vector2 startDir = (end + offset) - (bez2 + offset);
                startDir.Normalize();
                Vector2 basePoint = (end + offset) - (startDir * 5);

                if (Mathf.Sign((end.x + offset.x) - (basePoint.x)) == 1)
                {
                    Handles.Label(end + offset + new Vector2(-2, -8), ">", style);
                } else
                {
                    Handles.Label(end + offset + new Vector2(6, -8), "<", style);
                }
                break;
            }
            case HandleType.Label:
            {
                Handles.Label(start + offset, text, style);
                break;
            }
        }
    }
}
public enum HandleType
{
    Rectangle,
    Line,
    ArrowLine,
    Label
}