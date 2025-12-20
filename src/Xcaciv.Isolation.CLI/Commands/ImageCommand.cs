using System.CommandLine;
using Spectre.Console;
using Xcaciv.Isolation.Core.Interfaces;

namespace Xcaciv.Isolation.CLI.Commands;

internal static class ImageCommand
{
    public static Command Create(IImageManager imageManager)
    {
        var command = new Command("image", "Manage container images");

        command.AddCommand(CreateListCommand(imageManager));
        command.AddCommand(CreateImportCommand(imageManager));
        command.AddCommand(CreateRemoveCommand(imageManager));
        command.AddCommand(CreateInspectCommand(imageManager));

        return command;
    }

    private static Command CreateListCommand(IImageManager imageManager)
    {
        var command = new Command("list", "List all container images");
        command.AddAlias("ls");

        command.SetHandler(async () =>
        {
            try
            {
                var images = await imageManager.ListImagesAsync();

                if (images.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No images found[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Name:Tag");
                table.AddColumn("Image ID");
                table.AddColumn("Size");
                table.AddColumn("Created");
                table.AddColumn("Layers");

                foreach (var image in images)
                {
                    var size = image.SizeBytes.HasValue
                        ? FormatBytes(image.SizeBytes.Value)
                        : "N/A";

                    var created = image.CreatedAt.HasValue
                        ? image.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm")
                        : "N/A";

                    table.AddRow(
                        $"{image.Name}:{image.Tag}",
                        image.Id[..12],
                        size,
                        created,
                        image.Layers.Length.ToString()
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

    private static Command CreateImportCommand(IImageManager imageManager)
    {
        var nameOption = new Option<string>(
            name: "--name",
            description: "Name for the image")
        {
            IsRequired = true
        };

        var tagOption = new Option<string>(
            name: "--tag",
            description: "Tag for the image")
        {
            IsRequired = false
        };
        tagOption.SetDefaultValue("latest");

        var basePathOption = new Option<string>(
            name: "--base",
            description: "Path to the base image directory")
        {
            IsRequired = true
        };

        var layersOption = new Option<string[]>(
            name: "--layer",
            description: "Layer directory paths (can be specified multiple times)")
        {
            AllowMultipleArgumentsPerToken = false,
            IsRequired = false
        };

        var command = new Command("import", "Import a container image from local directories");
        command.AddOption(nameOption);
        command.AddOption(tagOption);
        command.AddOption(basePathOption);
        command.AddOption(layersOption);

        command.SetHandler(async (string name, string tag, string basePath, string[]? layers) =>
        {
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Importing image...", async ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots);
                        
                        await imageManager.ImportImageAsync(
                            name,
                            tag,
                            basePath,
                            layers ?? Array.Empty<string>());

                        ctx.Status("Image imported successfully!");
                    });

                AnsiConsole.MarkupLine($"[green]Image '{name}:{tag}' imported successfully[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, nameOption, tagOption, basePathOption, layersOption);

        return command;
    }

    private static Command CreateRemoveCommand(IImageManager imageManager)
    {
        var imageArgument = new Argument<string>(
            name: "image",
            description: "Image name with optional tag (name:tag)");

        var command = new Command("remove", "Remove a container image");
        command.AddAlias("rm");
        command.AddArgument(imageArgument);

        command.SetHandler(async (string image) =>
        {
            try
            {
                await imageManager.RemoveImageAsync(image);
                AnsiConsole.MarkupLine($"[green]Image '{image}' removed successfully[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, imageArgument);

        return command;
    }

    private static Command CreateInspectCommand(IImageManager imageManager)
    {
        var imageArgument = new Argument<string>(
            name: "image",
            description: "Image name with optional tag (name:tag)");

        var command = new Command("inspect", "Display detailed information about an image");
        command.AddArgument(imageArgument);

        command.SetHandler(async (string image) =>
        {
            try
            {
                var imageInfo = await imageManager.GetImageAsync(image);

                if (imageInfo is null)
                {
                    AnsiConsole.MarkupLine($"[red]Image '{image}' not found[/]");
                    return;
                }

                var panel = new Panel(CreateImageDetailsTable(imageInfo))
                {
                    Header = new PanelHeader($"Image: {imageInfo.Name}:{imageInfo.Tag}"),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            }
        }, imageArgument);

        return command;
    }

    private static Table CreateImageDetailsTable(Core.Models.ContainerImage image)
    {
        var table = new Table();
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("[bold]ID[/]", image.Id);
        table.AddRow("[bold]Name[/]", image.Name);
        table.AddRow("[bold]Tag[/]", image.Tag);
        table.AddRow("[bold]Base Path[/]", image.BasePath);
        table.AddRow("[bold]Layers[/]", image.Layers.Length.ToString());

        if (image.SizeBytes.HasValue)
        {
            table.AddRow("[bold]Size[/]", FormatBytes(image.SizeBytes.Value));
        }

        if (image.CreatedAt.HasValue)
        {
            table.AddRow("[bold]Created[/]", image.CreatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        }

        if (image.Metadata is not null && image.Metadata.Count > 0)
        {
            table.AddRow("[bold]Metadata[/]", String.Empty);
            foreach (var kvp in image.Metadata)
            {
                table.AddRow($"  {kvp.Key}", kvp.Value);
            }
        }

        return table;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
