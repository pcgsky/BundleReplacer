using BundleReplacer.Helper;
using Mono.Options;

namespace BundleReplacer.Commands;

public class Texture2DExportCommand : Command
{
    private string? bundlePath;
    private string? outputDir;

    public Texture2DExportCommand() : base("export-texture-2ds", "Export Texture2Ds from a bundle file")
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
        ExportCommand.ExtractBundle(bundlePath, outputDir, new BundleReplaceHelper.Filter() { Texture2D = true });

        return 0;
    }
}
