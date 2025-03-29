using BundleReplacer.Helper;
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
        ExportCommand.ExtractBundle(bundlePath, outputDir, new BundleReplaceHelper.Filter() { MonoBehaviour = true });

        return 0;
    }
}
