using System.CommandLine;
using Xcaciv.Isolation.CLI.Commands;
using Xcaciv.Isolation.Core.Services;

namespace Xcaciv.Isolation.CLI;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var containerManager = new HcsContainerManager();

        var rootCommand = new RootCommand("Windows Container Manager - Manage Windows containers using HCS API")
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

