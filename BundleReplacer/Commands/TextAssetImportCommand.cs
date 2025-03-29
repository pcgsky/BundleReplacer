using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Mono.Options;
using BundleHelper = BundleReplacer.Helper.BundleHelper;

namespace BundleReplacer.Commands;

public class TextAssetImportCommand : Command
{
    private string? bundlePath;
    private string? replaceDir;
    private string? outputPath;

    public TextAssetImportCommand() : base("import-text-assets", "Import TextAssets into a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original bundle file", v => bundlePath = v},
            {"r|replace=", "The path to the binary files to replace", v => replaceDir = v},
            {"o|output=", "The output path", v => outputPath = v}
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(bundlePath) || string.IsNullOrWhiteSpace(replaceDir) || string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Missing required arguments");
        }
        ImportBundle(bundlePath, replaceDir, outputPath);

        return 0;
    }

    public static void ImportBundle(string bundlePath, string replaceDir, string outputPath)
    {
        var manager = new AssetsManager();
        var bundle = manager.LoadBundleFile(bundlePath);
        var asset = manager.LoadAssetsFileFromBundle(bundle, 0);

        var textAssets = asset.file.GetAssetsOfType(AssetClassID.TextAsset);
        if (textAssets.Count == 0) { return; }

        bool changed = false;

        foreach (var textAsset in textAssets)
        {
            var textAssetInfo = manager.GetBaseField(asset, textAsset);

            var name = textAssetInfo["m_Name"].AsString;
            var id = textAsset.PathId;
            var binPath = $"{replaceDir}/{name}-{id:X16}.bin";

            if (!File.Exists(binPath)) { continue; }

            textAssetInfo["m_Script"].AsByteArray = File.ReadAllBytes(binPath);

            var bytes = textAssetInfo.WriteToByteArray();
            textAsset.Replacer = new ContentReplacerFromBuffer(bytes);

            changed = true;
        }

        if (!changed) { return; }

        bundle.file.BlockAndDirInfo.DirectoryInfos[0].SetNewData(asset.file);
        BundleHelper.CompressBundle(outputPath, manager, bundle);
    }
}
