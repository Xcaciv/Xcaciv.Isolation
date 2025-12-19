using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;
using Xcaciv.Isolation.Core.Models;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class StartCommand
{
    public static Command Create(IContainerManager containerManager)
    {
        var nameOption = new Option<string>(
            name: "--name",
            description: "Name for the container")
        {
            IsRequired = true
        };

        var execOption = new Option<string>(
            name: "--exec",
            description: "Path to the executable to run")
        {
            IsRequired = true
        };

        var argsOption = new Option<string[]?>(
            name: "--args",
            description: "Arguments to pass to the executable")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var workdirOption = new Option<string?>(
            name: "--workdir",
            description: "Working directory for the container");

        var memoryOption = new Option<long?>(
            name: "--memory",
            description: "Memory limit in MB");

        var cpuOption = new Option<int?>(
            name: "--cpu",
            description: "CPU limit as percentage (1-100)");

        var envOption = new Option<string[]?>(
            name: "--env",
            description: "Environment variables in KEY=VALUE format")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var command = new Command("start", "Start a new container")
        {
            nameOption,
            execOption,
            argsOption,
            workdirOption,
            memoryOption,
            cpuOption,
            envOption
        };

        command.SetHandler(async (string name, string exec, string[]? args, string? workdir, 
            long? memory, int? cpu, string[]? env) =>
        {
            try
            {
                var envVars = ParseEnvironmentVariables(env);

                var config = new ContainerConfiguration
                {
                    Name = name,
                    ExecutablePath = exec,
                    Arguments = args,
                    WorkingDirectory = workdir,
                    MemoryLimitMB = memory,
                    CpuLimitPercent = cpu,
                    EnvironmentVariables = envVars
                };

                AnsiConsole.Status()
                    .Start("Starting container...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        var result = containerManager.StartAsync(config).GetAwaiter().GetResult();
                        
                        ctx.Status("Container started successfully!");

                        var table = new Table();
                        table.AddColumn("Property");
                        table.AddColumn("Value");
                        
                        table.AddRow("Container ID", result.Id);
                        table.AddRow("Name", result.Name);
                        table.AddRow("State", result.State.ToString());
                        table.AddRow("Process ID", result.ProcessId?.ToString() ?? "N/A");
                        table.AddRow("Executable", result.ExecutablePath ?? "N/A");

                        AnsiConsole.Write(table);
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, nameOption, execOption, argsOption, workdirOption, memoryOption, cpuOption, envOption);

        return command;
    }

    private static Dictionary<string, string>? ParseEnvironmentVariables(string[]? envVars)
    {
        if (envVars is null || envVars.Length == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string>();
        foreach (var envVar in envVars)
        {
            var parts = envVar.Split('=', 2);
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
        }

        return result.Count > 0 ? result : null;
    }
}
