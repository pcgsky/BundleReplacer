using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class TextAsset
{
    public static bool Export(string outputDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo textAsset)
    {
        var textAssetInfo = manager.GetBaseField(asset, textAsset);

        var name = textAssetInfo["m_Name"].AsString;
        if (string.IsNullOrWhiteSpace(name))
        {
            var script = manager.GetBaseField(asset, textAssetInfo["m_Script"]["m_PathID"].AsLong);
            if (script is not null) { name = script["m_Name"].AsString; }
        }

        var id = textAsset.PathId;
        var binPath = $"{outputDir}/{name}-{id:X16}.bin";

        Directory.CreateDirectory(outputDir);
        File.WriteAllBytes(binPath, textAssetInfo["m_Script"].AsByteArray);

        return true;
    }

    public static bool Import(string replaceDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo textAsset)
    {
        var textAssetInfo = manager.GetBaseField(asset, textAsset);

        var name = textAssetInfo["m_Name"].AsString;
        var id = textAsset.PathId;
        var binPath = $"{replaceDir}/{name}-{id:X16}.bin";

        if (!File.Exists(binPath)) { return false; }

        textAssetInfo["m_Script"].AsByteArray = File.ReadAllBytes(binPath);

        var bytes = textAssetInfo.WriteToByteArray();
        textAsset.Replacer = new ContentReplacerFromBuffer(bytes);

        return true;
    }
}
