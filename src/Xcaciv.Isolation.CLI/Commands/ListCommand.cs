using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class ListCommand
{
    public static Command Create(IContainerManager containerManager)
    {
        var command = new Command("list", "List all containers");
        command.AddAlias("ls");

        command.SetHandler(async () =>
        {
            try
            {
                var containers = await containerManager.ListContainersAsync();

                if (containers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No containers found[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Container ID");
                table.AddColumn("Name");
                table.AddColumn("State");
                table.AddColumn("Process ID");
                table.AddColumn("Created");

                foreach (var container in containers)
                {
                    var stateColor = container.State.ToString() switch
                    {
                        "Running" => "green",
                        "Stopped" => "red",
                        "Paused" => "yellow",
                        _ => "gray"
                    };

                    table.AddRow(
                        container.Id[..8],
                        container.Name,
                        $"[{stateColor}]{container.State}[/]",
                        container.ProcessId?.ToString() ?? "N/A",
                        container.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                    );
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        });

        return command;
    }
}
