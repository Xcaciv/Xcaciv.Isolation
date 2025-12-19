using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class RemoveCommand
{
    public static Command Create(IContainerManager containerManager)
    {
        var containerIdArgument = new Argument<string>(
            name: "container-id",
            description: "ID of the container to remove");

        var forceOption = new Option<bool>(
            name: "--force",
            description: "Force remove a running container");
        
        forceOption.AddAlias("-f");

        var command = new Command("remove", "Remove a stopped container");
        command.AddAlias("rm");
        command.AddArgument(containerIdArgument);
        command.AddOption(forceOption);

        command.SetHandler(async (string containerId, bool force) =>
        {
            try
            {
                if (force)
                {
                    // Stop first if forcing
                    try
                    {
                        await containerManager.StopAsync(containerId);
                    }
                    catch
                    {
                        // Ignore if already stopped
                    }
                }

                await AnsiConsole.Status()
                    .StartAsync("Removing container...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        await containerManager.RemoveAsync(containerId);
                        ctx.Status("Container removed successfully!");
                    });

                AnsiConsole.MarkupLine($"[green]Container {containerId} removed successfully[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, containerIdArgument, forceOption);

        return command;
    }
}
