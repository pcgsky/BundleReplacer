using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class MonoBehaviour
{
    public static bool Export(int index, string outputDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo monoBehaviour)
    {
        var monoBehaviourInfo = manager.GetBaseField(asset, monoBehaviour);
        var jsonPath = $"{outputDir}/{GetFileName(index, manager, bundle, asset, monoBehaviour, monoBehaviourInfo)}";

        Directory.CreateDirectory(outputDir);
        using var writer = new StreamWriter(jsonPath);
        AssetImportExport.DumpJsonAsset(writer, monoBehaviourInfo);

        return true;
    }

    public static bool Import(int index, string replaceDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo monoBehaviour)
    {
        var monoBehaviourInfo = manager.GetBaseField(asset, monoBehaviour);
        var jsonPath = $"{replaceDir}/{GetFileName(index, manager, bundle, asset, monoBehaviour, monoBehaviourInfo)}";

        if (!File.Exists(jsonPath)) { return false; }

        using var reader = new StreamReader(jsonPath);
        var template = manager.GetTemplateBaseField(asset, monoBehaviour);
        var bytes = AssetImportExport.ImportJsonAsset(template, reader, out var ex);
        monoBehaviour.Replacer = new ContentReplacerFromBuffer(bytes);

        return true;
    }

    public static string GetFileName(int index, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo monoBehaviour, AssetTypeValueField monoBehaviourInfo)
    {
        var name = monoBehaviourInfo["m_Name"].AsString;

        if (string.IsNullOrWhiteSpace(name))
        {
            var refAsset = asset;
            var fileID = monoBehaviourInfo["m_Script"]["m_FileID"].AsInt;
            if (fileID > 0)
            {
                var refFileName = asset.file.Metadata.Externals[fileID - 1].PathName;
                refAsset = manager.LoadAssetsFileFromBundle(bundle, Path.GetFileName(refFileName));
            }
            try
            {
                if (refAsset is not null)
                {
                    var script = manager.GetBaseField(refAsset, monoBehaviourInfo["m_Script"]["m_PathID"].AsLong);
                    if (script is not null) { name = script["m_Name"].AsString; }
                }
            }
            catch { }
        }

        var id = monoBehaviour.PathId;
        var jsonPath = $"{BundleReplaceHelper.EscapeFileName(name)}-{(index == 0 ? "" : $"{index}_")}{id:X16}.json";

        return jsonPath;
    }
}
