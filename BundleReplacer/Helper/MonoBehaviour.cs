using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class MonoBehaviour
{
    public static bool Export(string outputDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo monoBehaviour)
    {
        var monoBehaviourInfo = manager.GetBaseField(asset, monoBehaviour);

        var name = monoBehaviourInfo["m_Name"].AsString;
        if (string.IsNullOrWhiteSpace(name))
        {
            var script = manager.GetBaseField(asset, monoBehaviourInfo["m_Script"]["m_PathID"].AsLong);
            if (script is not null) { name = script["m_Name"].AsString; }
        }

        var id = monoBehaviour.PathId;
        var jsonPath = $"{outputDir}/{name}-{id:X16}.json";

        Directory.CreateDirectory(outputDir);
        using var writer = new StreamWriter(jsonPath);
        AssetImportExport.DumpJsonAsset(writer, monoBehaviourInfo);

        return true;
    }

    public static bool Import(string replaceDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo monoBehaviour)
    {
        var monoBehaviourInfo = manager.GetBaseField(asset, monoBehaviour);

        var name = monoBehaviourInfo["m_Name"].AsString;
        if (string.IsNullOrWhiteSpace(name))
        {
            var script = manager.GetBaseField(asset, monoBehaviourInfo["m_Script"]["m_PathID"].AsLong);
            if (script is not null) { name = script["m_Name"].AsString; }
        }

        var id = monoBehaviour.PathId;
        var jsonPath = $"{replaceDir}/{name}-{id:X16}.json";

        if (!File.Exists(jsonPath)) { return false; }

        using var reader = new StreamReader(jsonPath);
        var template = manager.GetTemplateBaseField(asset, monoBehaviour);
        var bytes = AssetImportExport.ImportJsonAsset(template, reader, out var ex);
        monoBehaviour.Replacer = new ContentReplacerFromBuffer(bytes);

        return true;
    }
}
