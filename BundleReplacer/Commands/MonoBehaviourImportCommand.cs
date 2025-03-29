using BundleReplacer.Helper;
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
        ImportCommand.ImportBundle(bundlePath, replaceDir, outputPath, new BundleReplaceHelper.Filter() { MonoBehaviour = true });

        return 0;
    }
}
