using System.CommandLine;
using Xcaciv.Isolation.CLI.Commands;
using Xcaciv.Isolation.Core.Services;

namespace Xcaciv.Isolation.CLI;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var containerManager = new WindowsContainerManager();

        var rootCommand = new RootCommand("Windows Container Manager - Manage Windows containers without Docker or Kubernetes")
        {
            StartCommand.Create(containerManager),
            StopCommand.Create(containerManager),
            ListCommand.Create(containerManager),
            InspectCommand.Create(containerManager),
            RemoveCommand.Create(containerManager)
        };

        return await rootCommand.InvokeAsync(args);
    }
}
