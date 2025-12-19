using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class InspectCommand
{
    public static Command Create(IContainerManager containerManager)
    {
        var containerIdArgument = new Argument<string>(
            name: "container-id",
            description: "ID of the container to inspect");

        var command = new Command("inspect", "Display detailed information about a container")
        {
            containerIdArgument
        };

        command.SetHandler(async (string containerId) =>
        {
            try
            {
                var container = await containerManager.GetContainerAsync(containerId);

                if (container is null)
                {
                    AnsiConsole.MarkupLine($"[red]Container {containerId} not found[/]");
                    return;
                }

                var panel = new Panel(CreateDetailsTable(container))
                {
                    Header = new PanelHeader($"Container: {container.Name}"),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, containerIdArgument);

        return command;
    }

    private static Table CreateDetailsTable(Core.Models.ContainerInfo container)
    {
        var table = new Table();
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("[bold]ID[/]", container.Id);
        table.AddRow("[bold]Name[/]", container.Name);
        
        var stateColor = container.State.ToString() switch
        {
            "Running" => "green",
            "Stopped" => "red",
            "Paused" => "yellow",
            _ => "gray"
        };
        table.AddRow("[bold]State[/]", $"[{stateColor}]{container.State}[/]");
        
        table.AddRow("[bold]Process ID[/]", container.ProcessId?.ToString() ?? "N/A");
        table.AddRow("[bold]Executable[/]", container.ExecutablePath ?? "N/A");
        table.AddRow("[bold]Root Path[/]", container.RootPath ?? "N/A");
        table.AddRow("[bold]Created[/]", container.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A");
        table.AddRow("[bold]Started[/]", container.StartedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A");
        
        if (container.MemoryLimitBytes.HasValue)
        {
            var memoryMB = container.MemoryLimitBytes.Value / 1024.0 / 1024.0;
            table.AddRow("[bold]Memory Limit[/]", $"{memoryMB:F2} MB");
        }
        else
        {
            table.AddRow("[bold]Memory Limit[/]", "No limit");
        }

        if (container.CpuLimitPercent.HasValue)
        {
            table.AddRow("[bold]CPU Limit[/]", $"{container.CpuLimitPercent.Value}%");
        }
        else
        {
            table.AddRow("[bold]CPU Limit[/]", "No limit");
        }

        return table;
    }
}
