using AssetsTools.NET;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BundleLoader
{
    public static class HeaderInformation
    {
        public static AssetsReplacer CreateHeaderInformation(Dictionary<AssetID, ulong> files, ulong pathId)
        {
            byte[] textAsset = null;
            using (MemoryStream ms = new MemoryStream())
            using (AssetsFileWriter writer = new AssetsFileWriter(ms))
            {
                writer.bigEndian = false;
                writer.WriteCountStringInt32("AssetMap");
                writer.Align();
                StringBuilder builder = new StringBuilder();
                foreach (KeyValuePair<AssetID, ulong> file in files)
                {
                    AssetID key = file.Key;
                    ulong value = file.Value;
                    builder.Append(key.fileName + "," + key.pathId + "=" + value + ";");
                }
                builder.Remove(builder.Length - 1, 1);
                writer.WriteCountStringInt32(builder.ToString());
                writer.Align();
                textAsset = ms.ToArray();
            }
            return new AssetsReplacerFromMemory(0, pathId, 0x31, 0xFFFF, textAsset);
        }
    }
}
