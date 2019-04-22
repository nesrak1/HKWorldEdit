using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BundleLoader
{
    //this class is here because I don't have the updated source code to assetstools.net rn
    public static class Extensions
    {
        //AssetsBundleFile
        public static void Write(this AssetsBundleFile file, AssetsFileWriter writer, byte[] assetsFileBinary/*AssetsFile assetsFile, AssetsReplacer[] replacers*/)
        {
            file.bundleHeader6.Write(writer, 0);
            file.bundleInf6.Write(writer, writer.Position);
            writer.Write(assetsFileBinary);
            //assetsFile.Write(writer, writer.Position, replacers, 0);
        }
        //AssetTypeInstance
        public static void Write(this AssetTypeInstance instance, AssetsFileWriter writer)
        {
            for (int i = 0; i < instance.baseFieldCount; i++)
            {
                instance.baseFields[i].Write(writer);
            }
        }
        //AssetTypeValueField
        public static void Write(this AssetTypeValueField valueField, AssetsFileWriter writer, int depth = 0)
        {
            if (valueField.templateField.isArray)
            {
                if (valueField.templateField.valueType == EnumValueTypes.ValueType_ByteArray)
                {
                    writer.Write(valueField.value.value.asByteArray.size);
                    writer.Write(valueField.value.value.asByteArray.data);
                    if (valueField.templateField.align) writer.Align();
                }
                else
                {
                    uint size = valueField.value.value.asArray.size;

                    writer.Write(size);
                    for (uint i = 0; i < size; i++)
                    {
                        valueField[i].Write(writer, depth + 1);
                    }
                    if (valueField.templateField.align) writer.Align();
                }
            }
            else
            {
                if (valueField.childrenCount == 0)
                {
                    switch (valueField.templateField.valueType)
                    {
                        case EnumValueTypes.ValueType_Int8:
                            writer.Write(valueField.value.value.asInt8);
                            if (valueField.templateField.align) writer.Align();
                            break;
                        case EnumValueTypes.ValueType_UInt8:
                            writer.Write(valueField.value.value.asUInt8);
                            if (valueField.templateField.align) writer.Align();
                            break;
                        case EnumValueTypes.ValueType_Bool:
                            writer.Write(valueField.value.value.asBool);
                            if (valueField.templateField.align) writer.Align();
                            break;
                        case EnumValueTypes.ValueType_Int16:
                            writer.Write(valueField.value.value.asInt16);
                            if (valueField.templateField.align) writer.Align();
                            break;
                        case EnumValueTypes.ValueType_UInt16:
                            writer.Write(valueField.value.value.asUInt16);
                            if (valueField.templateField.align) writer.Align();
                            break;
                        case EnumValueTypes.ValueType_Int32:
                            writer.Write(valueField.value.value.asInt32);
                            break;
                        case EnumValueTypes.ValueType_UInt32:
                            writer.Write(valueField.value.value.asUInt32);
                            break;
                        case EnumValueTypes.ValueType_Int64:
                            writer.Write(valueField.value.value.asInt64);
                            break;
                        case EnumValueTypes.ValueType_UInt64:
                            writer.Write(valueField.value.value.asUInt64);
                            break;
                        case EnumValueTypes.ValueType_Float:
                            writer.Write(valueField.value.value.asFloat);
                            break;
                        case EnumValueTypes.ValueType_Double:
                            writer.Write(valueField.value.value.asDouble);
                            break;
                        case EnumValueTypes.ValueType_String:
                            writer.WriteCountStringInt32(valueField.value.value.asString);
                            writer.Align();
                            break;
                    }
                }
                else
                {
                    for (uint i = 0; i < valueField.childrenCount; i++)
                    {
                        valueField[i].Write(writer, depth + 1);
                    }
                    if (valueField.templateField.align) writer.Align();
                }
            }
        }
        //AssetsFileTable
        public static List<AssetFileInfoEx> GetAssetsOfType(this AssetsFileTable table, int typeId)
        {
            List<AssetFileInfoEx> infos = new List<AssetFileInfoEx>();
            foreach (AssetFileInfoEx info in table.pAssetFileInfo)
            {
                if (info.curFileType == typeId)
                {
                    infos.Add(info);
                }
            }
            return infos;
        }
        //AssetsFile
        public static ulong WriteFix(this AssetsFile file, AssetsFileWriter writer, ulong filePos, AssetsReplacer[] pReplacers, uint fileID, ClassDatabaseFile typeMeta = null)
        {
            file.header.Write(writer.Position, writer);

            for (int i = 0; i < pReplacers.Length; i++)
            {
                AssetsReplacer replacer = pReplacers[i];
                if (!file.typeTree.pTypes_Unity5.Any(t => t.classId == replacer.GetClassID()))
                {
                    Type_0D type = new Type_0D()
                    {
                        classId = replacer.GetClassID(),
                        unknown16_1 = 0,
                        scriptIndex = 0xFFFF,
                        unknown5 = 0,
                        unknown6 = 0,
                        unknown7 = 0,
                        unknown8 = 0,
                        typeFieldsExCount = 0,
                        stringTableLen = 0,
                        pStringTable = ""
                    };
                    file.typeTree.pTypes_Unity5.Concat(new Type_0D[] { type });
                }
            }
            file.typeTree.Write(writer.Position, writer, file.header.format);

            int initialSize = (int)(AssetFileInfo.GetSize(file.header.format) * file.AssetCount);
            int newSize = (int)(AssetFileInfo.GetSize(file.header.format) * (file.AssetCount + pReplacers.Length));
            file.reader.Position = file.AssetTablePos;

            List<AssetFileInfo> originalAssetInfos = new List<AssetFileInfo>();
            List<AssetFileInfo> assetInfos = new List<AssetFileInfo>();
            List<AssetsReplacer> currentReplacers = pReplacers.ToList();
            uint currentOffset = 0;

            //-write all original assets, modify sizes if needed and skip those to be removed
            for (int i = 0; i < file.AssetCount; i++)
            {
                AssetFileInfo info = new AssetFileInfo();
                info.Read(file.header.format, file.reader.Position, file.reader, file.reader.bigEndian);
                originalAssetInfos.Add(info);
                AssetsReplacer replacer = currentReplacers.FirstOrDefault(n => n.GetPathID() == info.index);
                if (replacer != null)
                {
                    if (replacer.GetReplacementType() == AssetsReplacementType.AssetsReplacement_AddOrModify)
                    {
                        int classIndex = Array.FindIndex(file.typeTree.pTypes_Unity5, t => t.classId == replacer.GetClassID());
                        info = new AssetFileInfo()
                        {
                            index = replacer.GetPathID(),
                            offs_curFile = currentOffset,
                            curFileSize = (uint)classIndex,
                            curFileTypeOrIndex = (uint)replacer.GetClassID(),
                            inheritedUnityClass = (ushort)replacer.GetClassID(), //-what is this
                            scriptIndex = replacer.GetMonoScriptID(),
                            unknown1 = 0
                        };
                    }
                    else if (replacer.GetReplacementType() == AssetsReplacementType.AssetsReplacement_Remove)
                    {
                        continue;
                    }
                }
                currentOffset += info.curFileSize;
                uint pad = 8 - (currentOffset % 8);
                if (pad != 8) currentOffset += pad;

                assetInfos.Add(info);
            }

            //-write new assets
            while (currentReplacers.Count > 0)
            {
                AssetsReplacer replacer = currentReplacers.First();
                if (replacer.GetReplacementType() == AssetsReplacementType.AssetsReplacement_AddOrModify)
                {
                    int classIndex = Array.FindIndex(file.typeTree.pTypes_Unity5, t => t.classId == replacer.GetClassID());
                    AssetFileInfo info = new AssetFileInfo()
                    {
                        index = replacer.GetPathID(),
                        offs_curFile = currentOffset,
                        curFileSize = (uint)replacer.GetSize(),
                        curFileTypeOrIndex = (uint)classIndex,
                        inheritedUnityClass = (ushort)replacer.GetClassID(),
                        scriptIndex = replacer.GetMonoScriptID(),
                        unknown1 = 0
                    };
                    currentOffset += info.curFileSize;
                    uint pad = 8 - (currentOffset % 8);
                    if (pad != 8) currentOffset += pad;

                    assetInfos.Add(info);
                }
                currentReplacers.Remove(replacer);
            }

            writer.Write(assetInfos.Count);
            writer.Align();
            for (int i = 0; i < assetInfos.Count; i++)
            {
                assetInfos[i].Write(file.header.format, writer.Position, writer);
            }

            file.preloadTable.Write(writer.Position, writer, file.header.format);

            file.dependencies.Write(writer.Position, writer, file.header.format);

            uint metadataSize = (uint)writer.Position - 0x13;

            //-for padding only. if all initial data before assetData is more than 0x1000, this is skipped
            while (writer.Position < 0x1000/*header.offs_firstFile*/)
            {
                writer.Write((byte)0x00);
            }

            writer.Align8();

            file.header.offs_firstFile = (uint)writer.Position;

            for (int i = 0; i < assetInfos.Count; i++)
            {
                AssetFileInfo info = assetInfos[i];
                AssetsReplacer replacer = pReplacers.FirstOrDefault(n => n.GetPathID() == info.index);
                if (replacer != null)
                {
                    if (replacer.GetReplacementType() == AssetsReplacementType.AssetsReplacement_AddOrModify)
                    {
                        replacer.Write(writer.Position, writer);
                        writer.Align8();
                    }
                    else if (replacer.GetReplacementType() == AssetsReplacementType.AssetsReplacement_Remove)
                    {
                        continue;
                    }
                }
                else
                {
                    AssetFileInfo originalInfo = originalAssetInfos.FirstOrDefault(n => n.index == info.index);
                    if (originalInfo != null)
                    {
                        file.reader.Position = file.header.offs_firstFile + originalInfo.offs_curFile;
                        byte[] assetData = file.reader.ReadBytes((int)originalInfo.curFileSize);
                        writer.Write(assetData);
                        writer.Align8();
                    }
                }
            }

            ulong fileSizeMarker = writer.Position;

            file.reader.Position = file.header.offs_firstFile;

            writer.Position = 0;
            file.header.metadataSize = metadataSize;
            file.header.fileSize = (uint)fileSizeMarker;
            file.header.Write(writer.Position, writer);
            return writer.Position;
        }
    }
}
