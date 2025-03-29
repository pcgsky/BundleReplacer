using AssetsTools.NET.Extra;
using BundleReplacer.Helper;
using Mono.Options;
using static BundleReplacer.Helper.BundleReplaceHelper;

namespace BundleReplacer.Commands;

public class ImportCommand : Command
{
    private string? bundlePath;
    private string? replaceDir;
    private string? outputPath;
    private string? filterStr;

    public ImportCommand() : base("import", "Import into a bundle file")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original bundle file", v => bundlePath = v},
            {"r|replace=", "The path to the json files to replace", v => replaceDir = v},
            {"o|output=", "The output path", v => outputPath = v},
            {"f|filter=", "Filter, including: MonoBehaviour, TextAsset, Texture2D", v => filterStr = v },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(bundlePath) || string.IsNullOrWhiteSpace(replaceDir) || string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Missing required arguments");
        }
        ImportBundle(bundlePath, replaceDir, outputPath, filterStr ?? "");

        return 0;
    }

    public static void ImportBundle(string bundlePath, string replaceDir, string outputPath, string filterStr) => ImportBundle(bundlePath, replaceDir, outputPath, ParseFilter(filterStr));

    internal static void ImportBundle(string bundlePath, string replaceDir, string outputPath, Filter filter)
    {
        var manager = new AssetsManager();
        var bundle = manager.LoadBundleFile(bundlePath);
        var assets = manager.LoadAssetsFileFromBundle(bundle, 0);

        bool changed = false;
        foreach (var info in assets.file.Metadata.AssetInfos)
        {
            switch (info.TypeId)
            {
                case (int)AssetClassID.MonoBehaviour:
                    if (filter.MonoBehaviour) { changed = changed || MonoBehaviour.Import(replaceDir, manager, bundle, assets, info); }
                    break;
                case (int)AssetClassID.TextAsset:
                    if (filter.TextAsset) { changed = changed || TextAsset.Import(replaceDir, manager, bundle, assets, info); }
                    break;
                case (int)AssetClassID.Texture2D:
                    if (filter.Texture2D) { changed = changed || Texture2D.Import(replaceDir, manager, bundle, assets, info); }
                    break;
            }
        }

        if (!changed) { return; }

        bundle.file.BlockAndDirInfo.DirectoryInfos[0].SetNewData(assets.file);
        CompressBundle(outputPath, manager, bundle);
    }
}
