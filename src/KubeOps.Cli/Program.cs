using System.CommandLine;

await new RootCommand(
        "CLI for KubeOps. Commandline tool to help with management tasks such as generating or installing CRDs.")
    {
        KubeOps.Cli.Commands.Utilities.Version.Command(),
    }
    .InvokeAsync(args);
