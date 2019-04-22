using AssetsTools.NET;
using BundleLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BundleLoader
{
    public static class BundleMeta
    {
        public static AssetsReplacer CreateBundleInformation(Dictionary<AssetID, ulong> files, List<ulong> spriteIds, ulong pathId)
        {
            byte[] metaAsset = null;
            using (MemoryStream ms = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(ms))
            {
                writer.bigEndian = false;
                writer.Write(0);
                writer.Write((uint)(1 + files.Count + spriteIds.Count)); //spriteIds is risky, we don't know if all are going to be written

                writer.Write(0);
                writer.Write((ulong)2);
                foreach (KeyValuePair<AssetID, ulong> file in files)
                {
                    writer.Write(0);
                    writer.Write(file.Value);
                }
                writer.Align();

                foreach (KeyValuePair<AssetID, ulong> file in files)
                {
                    if (spriteIds.Contains(file.Value))
                    {
                        writer.Write(0);
                        writer.Write(file.Value);
                    }
                }
                writer.Align();

                int index = 1;
                writer.Write((uint)files.Count);
                foreach (KeyValuePair<AssetID, ulong> file in files)
                {
                    writer.WriteCountStringInt32(file.Key.fileName + "/" + file.Key.pathId + ".dat");
                    writer.Align();
                    writer.Write(index++);
                    writer.Write(1);
                    writer.Write(0);
                    writer.Write(file.Value);
                }
                writer.Align();

                writer.Write(0);
                writer.Write(1);
                writer.Write(0);
                writer.Write((ulong)2);

                writer.Write((uint)1);

                writer.Write(0);

                writer.Write(0);

                writer.Write((uint)0);

                writer.Write((uint)0);

                writer.Write(7);

                writer.Write(0);

                metaAsset = ms.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0x8E, 0xFFFF, metaAsset);
        }
    }
}
