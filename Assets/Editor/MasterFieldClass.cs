using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public struct FsmTransition
{
    public FsmEvent fsmEvent;
    public string toState;
    public int linkStyle;
    public int linkConstraint;
    public byte colorIndex;
    public FsmTransition(AssetTypeValueField valueField)
    {
        fsmEvent = new FsmEvent(valueField.Get("fsmEvent"));
        toState = valueField.Get("toState").GetValue().AsString();
        linkStyle = valueField.Get("linkStyle").GetValue().AsInt();
        linkConstraint = valueField.Get("linkConstraint").GetValue().AsInt();
        colorIndex = (byte)valueField.Get("colorIndex").GetValue().AsInt();
    }
}

public struct FsmEvent
{
    public string name;
    public bool isSystemEvent;
    public bool isGlobal;
    public FsmEvent(AssetTypeValueField valueField)
    {
        name = valueField.Get("name").GetValue().AsString();
        isSystemEvent = valueField.Get("isSystemEvent").GetValue().AsBool();
        isGlobal = valueField.Get("isGlobal").GetValue().AsBool();
    }
}

public enum ParamDataType
{
    Integer,
    Boolean,
    Float,
    String,
    Color,
    ObjectReference,
    LayerMask,
    Enum,
    Vector2,
    Vector3,
    Vector4,
    Rect,
    Array,
    Character,
    AnimationCurve,
    FsmFloat,
    FsmInt,
    FsmBool,
    FsmString,
    FsmGameObject,
    FsmOwnerDefault,
    FunctionCall,
    FsmAnimationCurve,
    FsmEvent,
    FsmObject,
    FsmColor,
    Unsupported,
    GameObject,
    FsmVector3,
    LayoutOption,
    FsmRect,
    FsmEventTarget,
    FsmMaterial,
    FsmTexture,
    Quaternion,
    FsmQuaternion,
    FsmProperty,
    FsmVector2,
    FsmTemplateControl,
    FsmVar,
    CustomClass,
    FsmArray,
    FsmEnum
}