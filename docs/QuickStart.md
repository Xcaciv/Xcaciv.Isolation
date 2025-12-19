# Quick Start Guide

Get started with Xcaciv.Isolation in minutes!

## Installation

### Option 1: Build from Source

1. Clone the repository:
   ```powershell
   git clone https://github.com/Xcaciv/Xcaciv.Isolation.git
   cd Xcaciv.Isolation
   ```

2. Build the project:
   ```powershell
   dotnet build --configuration Release
   ```

3. The executable will be at:
   ```
   src\Xcaciv.Isolation.CLI\bin\Release\net10.0\Xcaciv.Isolation.CLI.exe
   ```

### Option 2: Build NativeAOT (Compact)

For a self-contained executable:

1. Build with the Compact configuration:
   ```powershell
   dotnet publish src\Xcaciv.Isolation.CLI\Xcaciv.Isolation.CLI.csproj `
       -c Release `
       -p:PublishAot=true `
       -p:PublishTrimmed=true `
       -p:PublishSingleFile=true `
       -p:SelfContained=true `
       -r win-x64
   ```

2. The self-contained executable will be at:
   ```
   src\Xcaciv.Isolation.CLI\bin\Release\net10.0\win-x64\publish\Xcaciv.Isolation.CLI.exe
   ```

## First Container

Let's start your first container!

### Step 1: Check the CLI

```powershell
.\Xcaciv.Isolation.CLI.exe --help
```

You should see the available commands.

### Step 2: Start a Simple Container

Run Notepad in a container:

```powershell
.\Xcaciv.Isolation.CLI.exe start --name my-first-container --exec C:\Windows\System32\notepad.exe
```

You'll see output like:
```
┌────────────┬──────────────────────────────────┐
│ Property   │ Value                            │
├────────────┼──────────────────────────────────┤
│ Container  │ abc123def456...                  │
│ ID         │                                  │
│ Name       │ my-first-container               │
│ State      │ Running                          │
│ Process ID │ 12345                            │
│ Executable │ C:\Windows\System32\notepad.exe  │
└────────────┴──────────────────────────────────┘
```

Notepad should open in a new window!

### Step 3: List Your Containers

```powershell
.\Xcaciv.Isolation.CLI.exe list
```

or use the short alias:

```powershell
.\Xcaciv.Isolation.CLI.exe ls
```

### Step 4: Inspect the Container

Copy the Container ID from the list (first column) and inspect it:

```powershell
.\Xcaciv.Isolation.CLI.exe inspect <container-id>
```

Example:
```powershell
.\Xcaciv.Isolation.CLI.exe inspect abc123de
```

### Step 5: Stop the Container

Close Notepad or stop it via CLI:

```powershell
.\Xcaciv.Isolation.CLI.exe stop abc123de
```

### Step 6: Remove the Container

Clean up by removing the stopped container:

```powershell
.\Xcaciv.Isolation.CLI.exe remove abc123de
```

or use the short alias with force:

```powershell
.\Xcaciv.Isolation.CLI.exe rm abc123de -f
```

## Try Resource Limits

### Memory Limit Example

```powershell
.\Xcaciv.Isolation.CLI.exe start `
    --name memory-limited `
    --exec C:\Windows\System32\notepad.exe `
    --memory 100
```

This limits Notepad to 100 MB of memory.

### CPU Limit Example

```powershell
.\Xcaciv.Isolation.CLI.exe start `
    --name cpu-limited `
    --exec C:\Windows\System32\cmd.exe `
    --args /k "echo Limited to 25% CPU" `
    --cpu 25
```

This limits the process to 25% CPU usage.

## Next Steps

Now that you've started your first container, explore more:

1. **Read the Examples**: Check out `docs/Examples.md` for more use cases
2. **Understand the Architecture**: See `docs/Architecture.md` for technical details
3. **Run Your Own Apps**: Try containerizing your applications!

## Common First-Time Issues

### "Access Denied" Error

You need to run as Administrator. Right-click PowerShell and select "Run as Administrator".

### "Executable not found" Error

Make sure the path to your executable is correct and the file exists. Use full paths:

```powershell
# Good
--exec C:\Windows\System32\notepad.exe

# Might not work
--exec notepad.exe
```

### Container Stops Immediately

Some programs exit immediately. Try with long-running programs like:
- `notepad.exe`
- `cmd.exe` with `/k` argument
- Your own applications

## Getting Help

For each command, use `--help`:

```powershell
.\Xcaciv.Isolation.CLI.exe start --help
.\Xcaciv.Isolation.CLI.exe stop --help
.\Xcaciv.Isolation.CLI.exe list --help
.\Xcaciv.Isolation.CLI.exe inspect --help
.\Xcaciv.Isolation.CLI.exe remove --help
```

## What's Next?

- Run multiple containers simultaneously
- Test your applications under resource constraints
- Isolate different development environments
- Create scripts to automate container management

Happy containerizing! 🚀
