using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class LogsCommand
{
    public static Command Create(IContainerLogger logger)
    {
        var containerIdArgument = new Argument<string>(
            name: "container-id",
            description: "ID of the container");

        var followOption = new Option<bool>(
            name: "--follow",
            description: "Follow log output");
        
        followOption.AddAlias("-f");

        var tailOption = new Option<int?>(
            name: "--tail",
            description: "Number of lines to show from the end of the logs");

        var command = new Command("logs", "View container logs");
        command.AddArgument(containerIdArgument);
        command.AddOption(followOption);
        command.AddOption(tailOption);

        command.SetHandler(async (string containerId, bool follow, int? tail) =>
        {
            try
            {
                AnsiConsole.MarkupLine($"[bold]Logs for container {containerId}[/]");
                AnsiConsole.WriteLine();

                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                await foreach (var logEntry in logger.GetLogsAsync(containerId, follow, tail, cts.Token))
                {
                    var timestamp = logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var stream = logEntry.Stream == Core.Models.LogStream.StdOut ? "[green]OUT[/]" : "[red]ERR[/]";
                    
                    AnsiConsole.MarkupLine($"[grey]{timestamp}[/] {stream} {logEntry.Message.EscapeMarkup()}");
                }

                if (follow && !cts.Token.IsCancellationRequested)
                {
                    AnsiConsole.MarkupLine("[yellow]End of log stream[/]");
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]Log streaming cancelled[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, containerIdArgument, followOption, tailOption);

        return command;
    }
}
