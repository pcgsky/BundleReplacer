using AssetsTools.NET.Extra;
using Mono.Options;

namespace BundleReplacer.Commands;

public class MonoBehaviourExportCommand : Command
{
    private string? bundlePath;
    private string? outputDir;

    public MonoBehaviourExportCommand() : base("export-mono-behaviours", "Export MonoBehaviours from a bundle file")
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

        var monoBehaviours = asset.file.GetAssetsOfType(AssetClassID.MonoBehaviour);
        if (monoBehaviours.Count == 0) { return; }

        Directory.CreateDirectory(outputDir);

        foreach (var monoBehaviour in monoBehaviours)
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

            using var writer = new StreamWriter(jsonPath);
            AssetImportExport.DumpJsonAsset(writer, monoBehaviourInfo);
        }
    }
}
