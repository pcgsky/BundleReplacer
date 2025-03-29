using Mono.Options;
using BundleReplaceHelper = BundleReplacer.Helper.BundleReplaceHelper;

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
        ImportCommand.ImportBundle(bundlePath, replaceDir, outputPath, new BundleReplaceHelper.Filter() { TextAsset = true });

        return 0;
    }

}
