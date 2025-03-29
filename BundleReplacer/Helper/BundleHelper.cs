using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace BundleReplacer.Helper;

internal static class BundleHelper
{
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
            bundle.file.Pack(writer, AssetBundleCompressionType.LZMA);
        }
        manager.UnloadAll();
        File.Delete(outputPath + ".tmp");
    }
}
