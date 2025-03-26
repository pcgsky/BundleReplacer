using BundleReplacer.Commands;
using Mono.Options;

CommandSet commands = new("BundleReplacer")
{
    new MonoBehaviourExportCommand(),
    new MonoBehaviourImportCommand(),
    new TextAssetExportCommand(),
    new TextAssetImportCommand(),
};

return commands.Run(args);
