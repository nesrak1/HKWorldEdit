using AssetsTools.NET;

public class UABEExtras
{
    public static string GetAssetNameFast(AssetFileInfoEx afi, ClassDatabaseFile cldb, ClassDatabaseType type, AssetsFileReader reader)
    {
        if (type.fields.Count == 0) return type.name.GetString(cldb);
        if (type.fields[1].fieldName.GetString(cldb) == "m_Name")
        {
            reader.Position = afi.absoluteFilePos;
            return reader.ReadCountStringInt32();
        }
        else if (type.name.GetString(cldb) == "GameObject")
        {
            reader.Position = afi.absoluteFilePos;
            int size = reader.ReadInt32();
            reader.Position += (ulong)(size * 12);
            reader.Position += 4;
            return reader.ReadCountStringInt32();
        }
        else if (type.name.GetString(cldb) == "MonoBehaviour")
        {
            reader.Position = afi.absoluteFilePos;
            reader.Position += 28;
            string name = reader.ReadCountStringInt32();
            if (name != "")
            {
                return name;
            }
        }
        return type.name.GetString(cldb);
    }
}
