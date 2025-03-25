using BundleReplacer.Commands;
using Mono.Options;

CommandSet commands = new("BundleReplacer")
{
    new MonoBehaviourExportCommand(),
    new MonoBehaviourImportCommand(),
};

return commands.Run(args);
