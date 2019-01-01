using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class HKMenu : EditorWindow
{
    AssetsManager am;
    List<Node> nodes = new List<Node>();
    List<HandleElement> elements = new List<HandleElement>();
    //[MenuItem("HKEdit/Show Menu")]
    //public static void ShowWindow()
    //{
    //    GetWindow(typeof(HKMenu));
    //}

    int dataVersion = -1;

    public void LoadFSM(AssetsManager am, AssetInfo info, string folderPath)
    {
        //risky but would technically work
        AssetsFileInstance assetsFileInstance = am.files.First();
        AssetsFile assetsFile = assetsFileInstance.file;
        AssetsFileTable assetsTable = assetsFileInstance.table;

        AssetFileInfoEx afi = assetsTable.getAssetInfo(info.id);

        AssetTypeValueField baseField = Util.GetMonoBaseField(am, assetsFile, afi, folderPath);

        AssetTypeValueField fsm = baseField.Get("fsm");
        AssetTypeValueField states = fsm.Get("states");
        dataVersion = fsm.Get("dataVersion").GetValue().AsInt();
        for (int i = 0; i < states.GetValue().AsArray().size; i++)
        {
            AssetTypeValueField state = states.Get((uint)i);

            string name = state.Get("name").GetValue().AsString();
            AssetTypeValueField rect = state.Get("position");
            float x = Mathf.Floor(rect.Get("x").GetValue().AsFloat());
            float y = Mathf.Floor(rect.Get("y").GetValue().AsFloat());
            float width = Mathf.Floor(rect.Get("width").GetValue().AsFloat());
            float height = Mathf.Floor(rect.Get("height").GetValue().AsFloat());

            HandleElement rectElem = new HandleElement(HandleType.Rectangle, new Vector2(x, y), new Vector2(x + width, y + height), new Color(0.5f, 0.5f, 0.5f, 0.7f));
            elements.Add(rectElem);
            elements.Add(new HandleElement(HandleType.Label, new Vector2(x + (width / 2), y + 3), (int)width, name));

            AssetTypeValueField transitions = state.Get("transitions");
            uint transitionCount = transitions.GetValue().AsArray().size;
            FsmTransition[] dotNetTransitions = new FsmTransition[transitionCount];
            for (int j = 0; j < transitionCount; j++)
            {
                dotNetTransitions[j] = new FsmTransition(transitions.Get((uint)j));
                string transitionName = dotNetTransitions[j].fsmEvent.name;
                elements.Add(new HandleElement(HandleType.Label, new Vector2(x + (width / 2), y + 3 + ((j + 1) * 16)), (int)width, transitionName));
            }

            Node node = new Node(name, new Rect(x, y, width, height), dotNetTransitions, rectElem);
            nodes.Add(node);

            //AssetTypeValueField transitions = state.Get("transitions");
            //uint transitionCount = transitions.GetValue().AsArray().size;
            //FsmTransition[] dotNetTransitions = new FsmTransition[transitionCount];
            //for (int j = 0; j < transitionCount; j++)
            //{
            //    dotNetTransitions[j] = new FsmTransition(transitions.Get((uint)j));
            //}
            //Node node = new Node(state, name, dotNetRect, dotNetTransitions);
            //nodes.Add(node);

            //node.grid.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
            //{
            //    foreach (Node node2 in nodes)
            //    {
            //        node2.Selected = false;
            //    }
            //    node.Selected = true;
            //    SidebarData(node);
            //};
            //
            //graphCanvas.Children.Add(node.grid);
        }

        foreach (Node node in nodes)
        {
            if (node.transitions.Length > 0)
            {
                float yPos = 25;
                foreach (FsmTransition trans in node.transitions)
                {
                    Node endNode = nodes.Where(n => n.text == trans.toState).FirstOrDefault();
                    if (endNode != null)
                    {
                        bool isLeft, dummy;
                        Vector2 start = ComputeLocation(node, endNode, yPos, out isLeft);
                        Vector2 end = ComputeLocation(endNode, node, 10, out dummy);

                        Vector2 startMiddle, endMiddle;
                        float dist = 70;
                        if (!isLeft)
                        {
                            startMiddle = new Vector2(start.x - dist, start.y);
                            endMiddle = new Vector2(end.x + dist, end.y);
                        }
                        else
                        {
                            startMiddle = new Vector2(start.x + dist, start.y);
                            endMiddle = new Vector2(end.x - dist, end.y);
                        }

                        elements.Insert(0, new HandleElement(HandleType.ArrowLine, start, end, startMiddle, endMiddle));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(node.text + " failed to connect to " + trans.toState);
                    }
                    yPos += 16;
                }
            }
        }
    }

    private Vector2 ComputeLocation(Node node1, Node node2)
    {
        Rect nodetfm1 = node1.shape;
        Rect nodetfm2 = node2.shape;

        Vector2 loc = new Vector2
        {
            x = nodetfm1.x + (nodetfm1.width / 2),
            y = nodetfm1.y + (nodetfm1.height / 2)
        };

        bool overlapY = Mathf.Abs(nodetfm1.y - nodetfm2.y) < nodetfm1.height / 2;
        if (!overlapY)
        {
            bool above = nodetfm1.y < nodetfm2.y;
            if (above)
                loc.y += nodetfm1.height / 2;
            else
                loc.y -= nodetfm1.height / 2;
        }

        bool overlapX = Mathf.Abs(nodetfm1.x - nodetfm2.x) < nodetfm1.width / 2;
        if (!overlapX)
        {
            bool left = nodetfm1.x < nodetfm2.x;
            if (left)
                loc.x += nodetfm1.width / 2;
            else
                loc.x -= nodetfm1.width / 2;
            loc.y = nodetfm1.y + 6;
        }

        return loc;
    }
    private Vector2 ComputeLocation(Node node1, Node node2, float yPos, out bool isLeft)
    {
        Rect nodetfm1 = node1.shape;
        Rect nodetfm2 = node2.shape;

        Vector2 loc = new Vector2
        {
            x = nodetfm1.x + (nodetfm1.width / 2),
            y = nodetfm1.y + yPos
        };

        bool left = nodetfm1.x < nodetfm2.x;
        if (left)
            loc.x += nodetfm1.width / 2;
        else
            loc.x -= nodetfm1.width / 2;
        isLeft = left;

        return loc;
    }

    //elements.Add(new HandleElement(HandleType.Rectangle, new Vector2(20, 20), new Vector2(80, 40), Color.gray));
    //elements.Add(new HandleElement(HandleType.Label, new Vector2(35, 23), "Start"));

    int tab = -1;
    string[] tabs = new[] { "State", "Events", "Variables" };
    void OnGUI()
    {
        Event e = Event.current;
        foreach (HandleElement ele in elements)
        {
            ele.Draw();
        }

        GUI.BeginGroup(new Rect(
            position.width - 220, 10,
            210, position.height - 20
        ));
            tab = GUI.SelectionGrid(new Rect(
                0, 0,
                210, 20
            ), tab, tabs, 3);
        GUI.EndGroup();

        HandleInput(e);
    }

    Vector2 lastPos = Vector2.zero;
    Vector2 offset = Vector2.zero;
    bool mouseDown = false;
    Node selectedNode = null;
    void HandleInput(Event e)
    {
        if (e.isMouse)
        {
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 1)
                {
                    lastPos = e.mousePosition;
                    mouseDown = true;
                }
                else if (e.button == 0)
                {
                    Vector2 mousePos = e.mousePosition;
                    foreach (Node node in nodes)
                    {
                        if (node.shape.Contains(mousePos - offset))
                        {
                            foreach (Node node2 in nodes)
                            {
                                node2.Selected = false;
                            }
                            node.Selected = true;
                            selectedNode = node;
                            Repaint();
                            break;
                        }
                    }
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 1)
                {
                    mouseDown = false;
                }
            }
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                if (mouseDown == false)
                    return;
                if (e.button == 1)
                {
                    foreach (HandleElement ele in elements)
                    {
                        ele.Translate(e.mousePosition - lastPos);
                    }
                    offset += e.mousePosition - lastPos;
                    lastPos = e.mousePosition;
                    Repaint();
                }
            }
        }
    }

    void OnEnable()
    {
        titleContent.text = "FSM Viewer";
    }
}
