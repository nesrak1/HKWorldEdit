using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.IO;
using System.Linq;

public static class Util
{
    public static AssetTypeValueField GetMonoBaseField(AssetsManager am, AssetsFile af, AssetFileInfoEx afi, string folderPath)
    {
        ClassDatabaseType cldt = AssetHelper.FindAssetClassByID(am.classFile, afi.curFileType);
        AssetTypeTemplateField pBaseField = new AssetTypeTemplateField();
        pBaseField.FromClassDatabase(am.classFile, cldt, 0);
        AssetTypeInstance mainAti = new AssetTypeInstance(1, new[] { pBaseField }, af.reader, false, afi.absoluteFilePos);
        AssetTypeTemplateField[] desMonos;
        desMonos = TryDeserializeMono(mainAti, am, folderPath);
        if (desMonos != null)
        {
            AssetTypeTemplateField[] templateField = pBaseField.children.Concat(desMonos).ToArray();
            pBaseField.children = templateField;
            pBaseField.childrenCount = (uint)pBaseField.children.Length;

            mainAti = new AssetTypeInstance(1, new[] { pBaseField }, af.reader, false, afi.absoluteFilePos);
        }
        return mainAti.GetBaseField();
    }
    private static AssetTypeTemplateField[] TryDeserializeMono(AssetTypeInstance ati, AssetsManager am, string rootDir)
    {
        AssetTypeInstance scriptAti = am.GetExtAsset(am.files.First(), ati.GetBaseField().Get("m_Script")).instance;
        string scriptName = scriptAti.GetBaseField().Get("m_Name").GetValue().AsString();
        string assemblyName = scriptAti.GetBaseField().Get("m_AssemblyName").GetValue().AsString();
        string assemblyPath = Path.Combine(Path.Combine(rootDir, "Managed"), assemblyName);
        if (File.Exists(assemblyPath))
        {
            MonoClass mc = new MonoClass();
            mc.Read(scriptName, assemblyPath);
            return mc.children;
        }
        else
        {
            return null;
        }
    }
}
