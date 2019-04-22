using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public struct DiffFile
{
    public uint magic;
    public int version;
    public string unityCompiledVersion;
    public List<GameObjectChange> changes;
    public List<GameObjectAdd> adds;
    public List<GameObjectRemove> removes;
    public List<GameObjectInfo> infos;
    public void Write(BinaryWriter w)
    {
        w.Write(magic);
        w.Write(version);
        w.Write(unityCompiledVersion);
        w.Write(changes.Count);
        for (int i = 0; i < changes.Count; i++)
        {
            changes[i].Write(w);
        }
        w.Write(adds.Count);
        for (int i = 0; i < adds.Count; i++)
        {
            adds[i].Write(w);
        }
        w.Write(removes.Count);
        for (int i = 0; i < removes.Count; i++)
        {
            removes[i].Write(w);
        }
        w.Write(infos.Count);
        for (int i = 0; i < infos.Count; i++)
        {
            infos[i].Write(w);
        }
    }
}

public struct GameObjectChange
{
    public long pathId;
    //public string path;
    //public int hash;
    //public HashTolerance hashTolerance;
    public List<ComponentChangeOrAdd> changes;
    public List<ComponentRemove> removes;
    public void Write(BinaryWriter w)
    {
        w.Write(pathId);
        //w.Write(path);
        //w.Write(hash);
        //w.Write((int)hashTolerance);
        w.Write(changes.Count);
        for (int i = 0; i < changes.Count; i++)
        {
            changes[i].Write(w);
        }
        w.Write(removes.Count);
        for (int i = 0; i < removes.Count; i++)
        {
            removes[i].Write(w);
        }
    }
}

public struct ComponentChangeOrAdd
{
    public bool isNewComponent;
    public int componentIndex;
    public string componentType;
    public List<FieldChange> changes;
    public void Write(BinaryWriter w)
    {
        w.Write(isNewComponent);
        w.Write(componentIndex);
        w.Write(componentType);
        w.Write(changes.Count);
        for (int i = 0; i < changes.Count; i++)
        {
            changes[i].Write(w);
        }
    }
}

public struct ComponentRemove
{
    public int componentIndex;
    public void Write(BinaryWriter w)
    {
        w.Write(componentIndex);
    }
}

public struct FieldChange
{
    public string fieldPath;
    public string fieldType;
    public object data;
    public FieldChange(string fieldPath, object data)
    {
        this.fieldPath = fieldPath;
        this.data = data;

        if (data is bool)
            fieldType = "bool";
        else if (data is char)
            fieldType = "char";
        else if (data is double)
            fieldType = "double";
        else if (data is short)
            fieldType = "short";
        else if (data is int)
            fieldType = "int";
        else if (data is long)
            fieldType = "long";
        else if (data is float)
            fieldType = "float";
        else if (data is ushort)
            fieldType = "ushort";
        else if (data is uint)
            fieldType = "uint";
        else if (data is ulong)
            fieldType = "ulong";
        else
            fieldType = "unknown";
    }
    public void Write(BinaryWriter w)
    {
        w.Write(fieldPath);
        w.Write(fieldType);
        byte[] bytes;

        if (data is bool)
            bytes = BitConverter.GetBytes((bool)data);
        else if (data is char)
            bytes = BitConverter.GetBytes((char)data);
        else if (data is double)
            bytes = BitConverter.GetBytes((double)data);
        else if (data is short)
            bytes = BitConverter.GetBytes((short)data);
        else if (data is int)
            bytes = BitConverter.GetBytes((int)data);
        else if (data is long)
            bytes = BitConverter.GetBytes((long)data);
        else if (data is float)
            bytes = BitConverter.GetBytes((float)data);
        else if (data is ushort)
            bytes = BitConverter.GetBytes((ushort)data);
        else if (data is uint)
            bytes = BitConverter.GetBytes((uint)data);
        else if (data is ulong)
            bytes = BitConverter.GetBytes((ulong)data);
        else
            bytes = new byte[0];

        w.Write(bytes.Length);
        w.Write(bytes);
    }
}

public struct GameObjectAdd
{
    public ulong pathId;
    public ulong parentId;
    public bool goNew;
    public bool parentNew;
    public void Write(BinaryWriter w)
    {
        w.Write(pathId);
        w.Write(parentId);
        w.Write(goNew);
        w.Write(parentNew);
    }
}

public struct GameObjectRemove
{
    public ulong pathId;
    //public string path;
    //public int hash;
    //public HashTolerance hashTolerance;
    public void Write(BinaryWriter w)
    {
        w.Write(pathId);
        //w.Write(path);
        //w.Write(hash);
        //w.Write((int)hashTolerance);
    }
}

public struct GameObjectInfo
{
    public string name;
    public uint fileId;
    public ulong origPathId;
    public ulong pathId;
    public GameObjectInfo(string name, uint fileId, ulong origPathId, ulong pathId)
    {
        this.name = name;
        this.fileId = fileId;
        this.origPathId = origPathId;
        this.pathId = pathId;
    }
    public void Write(BinaryWriter w)
    {
        w.Write(name);
        w.Write(fileId);
        w.Write(origPathId);
        w.Write(pathId);
    }
}

//how to differenciate gameobjects of the same name
//values like position are compared to tell the difference
//since unity won't expose ids
/*public enum HashTolerance
{
    ComponentCount = 1,   //0b000001
    Components = 3,       //0b000011
    Transform = 4,        //0b000100
    RigidBody = 8,        //0b001000
    Colliders = 16,       //0b010000
    MonoBehaviours = 32,  //0b100000
    AllComponents = 63    //0b111111
}*/