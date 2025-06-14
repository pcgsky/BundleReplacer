using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class TextAsset
{
    public static bool Export(string outputDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo textAsset)
    {
        var textAssetInfo = manager.GetBaseField(asset, textAsset);
        var binPath = $"{outputDir}/{GetFileName(textAsset, textAssetInfo)}";

        Directory.CreateDirectory(outputDir);
        File.WriteAllBytes(binPath, textAssetInfo["m_Script"].AsByteArray);

        return true;
    }

    public static bool Import(string replaceDir, AssetsManager manager, BundleFileInstance bundle, AssetsFileInstance asset, AssetFileInfo textAsset)
    {
        var textAssetInfo = manager.GetBaseField(asset, textAsset);
        var binPath = $"{replaceDir}/{GetFileName(textAsset, textAssetInfo)}";

        if (!File.Exists(binPath)) { return false; }

        textAssetInfo["m_Script"].AsByteArray = File.ReadAllBytes(binPath);

        var bytes = textAssetInfo.WriteToByteArray();
        textAsset.Replacer = new ContentReplacerFromBuffer(bytes);

        return true;
    }

    public static string GetFileName(AssetFileInfo textAsset, AssetTypeValueField textAssetInfo)
    {
        var name = textAssetInfo["m_Name"].AsString;

        var id = textAsset.PathId;
        var binPath = $"{BundleReplaceHelper.EscapeFileName(name)}-{id:X16}.bin";

        return binPath;
    }
}
