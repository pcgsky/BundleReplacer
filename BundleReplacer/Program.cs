using BundleReplacer.Commands;
using Mono.Options;

CommandSet commands = new("BundleReplacer")
{
    new ExportCommand(),
    new ImportCommand(),
    new MonoBehaviourExportCommand(),
    new MonoBehaviourImportCommand(),
    new TextAssetExportCommand(),
    new TextAssetImportCommand(),
    new Texture2DExportCommand(),
    new Texture2DImportCommand(),
};

return commands.Run(args);
