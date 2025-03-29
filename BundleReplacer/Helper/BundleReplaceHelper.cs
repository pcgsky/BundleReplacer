using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class BundleReplaceHelper
{
    public class Filter
    {
        public bool MonoBehaviour = false;
        public bool TextAsset = false;
        public bool Texture2D = false;
    }

    public static void CompressBundle(string outputPath, AssetsManager manager, BundleFileInstance bundle)
    {

        try { Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!); } catch (Exception) { }
        using (var writer = new AssetsFileWriter(outputPath + ".tmp"))
        {
            bundle.file.Write(writer);
        }
        bundle = manager.LoadBundleFile(outputPath + ".tmp", true);
        using (var writer = new AssetsFileWriter(outputPath))
        {
            bundle.file.Pack(writer, AssetBundleCompressionType.LZ4);
        }
        manager.UnloadAll();
        File.Delete(outputPath + ".tmp");
    }

    public static Filter ParseFilter(string filterStr)
    {
        if (string.IsNullOrWhiteSpace(filterStr))
        {
            return new Filter()
            {
                MonoBehaviour = true,
                TextAsset = true,
                Texture2D = true,
            };
        }
        var list = filterStr.Split(',');
        var filter = new Filter();
        foreach (var item in list)
        {
            switch (item.Trim())
            {
                case "MonoBehaviour":
                case "json":
                    filter.MonoBehaviour = true;
                    break;
                case "TextAsset":
                case "txt":
                case "bin":
                    filter.TextAsset = true;
                    break;
                case "Texture2D":
                case "png":
                    filter.Texture2D = true;
                    break;
            }
        }
        return filter;
    }
}
