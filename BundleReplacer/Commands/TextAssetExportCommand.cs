using AssetsTools.NET.Extra;
using Mono.Options;

namespace BundleReplacer.Commands;

public class TextAssetExportCommand : Command
{
    private string? bundlePath;
    private string? outputDir;

    public TextAssetExportCommand() : base("export-text-assets", "Export TextAssets from a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the bundle file", v => bundlePath = v},
            {"o|output=", "The output directory", v => outputDir = v}
        };
    }


    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(bundlePath) || string.IsNullOrWhiteSpace(outputDir))
        {
            throw new ArgumentException("Missing required arguments");
        }
        ExtractBundle(bundlePath, outputDir);

        return 0;
    }

    public static void ExtractBundle(string bundlePath, string outputDir)
    {
        var manager = new AssetsManager();
        var bundle = manager.LoadBundleFile(bundlePath);
        var asset = manager.LoadAssetsFileFromBundle(bundle, 0);

        var textAssets = asset.file.GetAssetsOfType(AssetClassID.TextAsset);
        if (textAssets.Count == 0) { return; }

        Directory.CreateDirectory(outputDir);

        foreach (var textAsset in textAssets)
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

            File.WriteAllBytes(binPath, textAssetInfo["m_Script"].AsByteArray);
        }
    }
}
