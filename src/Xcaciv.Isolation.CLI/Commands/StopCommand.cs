using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class StopCommand
{
    public static Command Create(IContainerManager containerManager)
    {
        var containerIdArgument = new Argument<string>(
            name: "container-id",
            description: "ID of the container to stop");

        var command = new Command("stop", "Stop a running container")
        {
            containerIdArgument
        };

        command.SetHandler(async (string containerId) =>
        {
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Stopping container...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        await containerManager.StopAsync(containerId);
                        ctx.Status("Container stopped successfully!");
                    });

                AnsiConsole.MarkupLine($"[green]Container {containerId} stopped successfully[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, containerIdArgument);

        return command;
    }
}
