using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Mono.Options;

namespace BundleReplacer.Commands;

public class MonoBehaviourImportCommand : Command
{
    private string? bundlePath;
    private string? replaceDir;
    private string? outputPath;

    public MonoBehaviourImportCommand() : base("import-mono-behaviours", "Import MonoBehaviours into a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original bundle file", v => bundlePath = v},
            {"r|replace=", "The path to the json files to replace", v => replaceDir = v},
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

        var monoBehaviours = asset.file.GetAssetsOfType(AssetClassID.MonoBehaviour);
        if (monoBehaviours.Count == 0) { return; }

        bool changed = false;

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
            var jsonPath = $"{replaceDir}/{name}-{id:X16}.json";

            if (!File.Exists(jsonPath)) { continue; }

            using var reader = new StreamReader(jsonPath);
            var template = manager.GetTemplateBaseField(asset, monoBehaviour);
            var bytes = AssetImportExport.ImportJsonAsset(template, reader, out var ex);
            monoBehaviour.Replacer = new ContentReplacerFromBuffer(bytes);

            changed = true;
        }

        if (!changed) { return; }

        bundle.file.BlockAndDirInfo.DirectoryInfos[0].SetNewData(asset.file);

        using var writer = new AssetsFileWriter(outputPath);
        bundle.file.Pack(writer, AssetBundleCompressionType.LZMA);
    }
}
