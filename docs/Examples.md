# Example Usage Guide

This guide demonstrates how to use Xcaciv.Isolation to manage Windows containers.

## Prerequisites

1. Windows 10/11 or Windows Server 2016+
2. Administrator privileges
3. Built Xcaciv.Isolation.CLI executable

## Basic Examples

### Example 1: Running Notepad in a Container

```powershell
# Start notepad in a container
.\Xcaciv.Isolation.CLI.exe start --name notepad-container --exec C:\Windows\System32\notepad.exe

# Output will show:
# Container ID: abc123...
# Name: notepad-container
# State: Running
# Process ID: 12345

# List all containers
.\Xcaciv.Isolation.CLI.exe list

# Inspect the container
.\Xcaciv.Isolation.CLI.exe inspect abc123

# Stop the container
.\Xcaciv.Isolation.CLI.exe stop abc123

# Remove the container
.\Xcaciv.Isolation.CLI.exe remove abc123
```

### Example 2: Running cmd.exe with Arguments

```powershell
# Start cmd.exe with specific arguments
.\Xcaciv.Isolation.CLI.exe start --name cmd-container --exec C:\Windows\System32\cmd.exe --args /k echo Hello from container!

# The container will run cmd.exe and execute the echo command
```

### Example 3: Container with Resource Limits

```powershell
# Start a container with memory and CPU limits
.\Xcaciv.Isolation.CLI.exe start `
    --name limited-container `
    --exec C:\MyApp\myapp.exe `
    --memory 512 `
    --cpu 25

# This container will be limited to:
# - 512 MB of memory
# - 25% CPU usage
```

### Example 4: Container with Environment Variables

```powershell
# Start a container with custom environment variables
.\Xcaciv.Isolation.CLI.exe start `
    --name envvar-container `
    --exec C:\MyApp\myapp.exe `
    --env MY_VAR=value1 `
    --env DEBUG=true `
    --env API_KEY=your-api-key

# The process will have access to these environment variables
```

### Example 5: Container with Working Directory

```powershell
# Start a container with a specific working directory
.\Xcaciv.Isolation.CLI.exe start `
    --name workdir-container `
    --exec C:\MyApp\myapp.exe `
    --workdir C:\MyApp\data `
    --args --input file.txt
```

## Testing the Container Isolation

To test the container isolation, you can:

1. **CPU Limiting Test**:
   ```powershell
   # Create a CPU-intensive PowerShell script
   Set-Content -Path C:\test-cpu.ps1 -Value @"
   while ($true) { 
       $x = 1..100000 | ForEach-Object { $_ * $_ }
   }
   "@

   # Run it in a container with CPU limit
   .\Xcaciv.Isolation.CLI.exe start `
       --name cpu-test `
       --exec powershell.exe `
       --args -File C:\test-cpu.ps1 `
       --cpu 25

   # Monitor CPU usage in Task Manager - should stay around 25%
   ```

2. **Memory Limiting Test**:
   ```powershell
   # Create a memory-intensive PowerShell script
   Set-Content -Path C:\test-memory.ps1 -Value @"
   `$array = @()
   while (`$true) {
       `$array += New-Object byte[] 10MB
       Start-Sleep -Seconds 1
   }
   "@

   # Run it in a container with memory limit
   .\Xcaciv.Isolation.CLI.exe start `
       --name memory-test `
       --exec powershell.exe `
       --args -File C:\test-memory.ps1 `
       --memory 256

   # The process should be terminated when it exceeds 256MB
   ```

## Advanced Examples

### Running a .NET Application

```powershell
# Start a .NET application in a container
.\Xcaciv.Isolation.CLI.exe start `
    --name dotnet-app `
    --exec C:\MyApp\MyApp.exe `
    --workdir C:\MyApp `
    --memory 1024 `
    --cpu 50 `
    --env ASPNETCORE_ENVIRONMENT=Development
```

### Running a Node.js Application

```powershell
# Start a Node.js application
.\Xcaciv.Isolation.CLI.exe start `
    --name nodejs-app `
    --exec C:\Program Files\nodejs\node.exe `
    --args C:\MyApp\server.js `
    --workdir C:\MyApp `
    --env NODE_ENV=production `
    --env PORT=3000
```

### Running a Python Script

```powershell
# Start a Python script
.\Xcaciv.Isolation.CLI.exe start `
    --name python-app `
    --exec C:\Python310\python.exe `
    --args C:\MyApp\script.py `
    --workdir C:\MyApp `
    --memory 512
```

## Managing Multiple Containers

```powershell
# Start multiple containers
.\Xcaciv.Isolation.CLI.exe start --name app1 --exec C:\App1\app1.exe
.\Xcaciv.Isolation.CLI.exe start --name app2 --exec C:\App2\app2.exe
.\Xcaciv.Isolation.CLI.exe start --name app3 --exec C:\App3\app3.exe

# List all containers
.\Xcaciv.Isolation.CLI.exe list

# Stop all containers (you'll need to script this)
# Get container IDs from list command, then:
.\Xcaciv.Isolation.CLI.exe stop <container-id-1>
.\Xcaciv.Isolation.CLI.exe stop <container-id-2>
.\Xcaciv.Isolation.CLI.exe stop <container-id-3>

# Remove all containers
.\Xcaciv.Isolation.CLI.exe remove <container-id-1>
.\Xcaciv.Isolation.CLI.exe remove <container-id-2>
.\Xcaciv.Isolation.CLI.exe remove <container-id-3>
```

## Force Removing a Running Container

```powershell
# If you need to remove a container that's still running
.\Xcaciv.Isolation.CLI.exe remove <container-id> --force

# Or using the alias
.\Xcaciv.Isolation.CLI.exe rm <container-id> -f
```

## Common Use Cases

### 1. Development Environment Isolation

Isolate development processes to prevent them from consuming all system resources:

```powershell
.\Xcaciv.Isolation.CLI.exe start `
    --name dev-server `
    --exec npm.cmd `
    --args run dev `
    --workdir C:\MyProject `
    --memory 1024 `
    --cpu 50
```

### 2. Testing with Resource Constraints

Test how your application behaves under resource constraints:

```powershell
.\Xcaciv.Isolation.CLI.exe start `
    --name test-low-memory `
    --exec C:\MyApp\myapp.exe `
    --memory 256 `
    --env TEST_MODE=constrained
```

### 3. Running Untrusted Code

Run potentially problematic code in isolation (note: this provides process-level isolation only):

```powershell
.\Xcaciv.Isolation.CLI.exe start `
    --name untrusted-app `
    --exec C:\Temp\unknown.exe `
    --memory 256 `
    --cpu 25
```

## Troubleshooting

### Container Won't Start

Check that:
1. The executable path is correct and the file exists
2. You have permissions to execute the file
3. You're running as Administrator
4. The executable is a valid Windows executable

### Container Immediately Stops

The process might be exiting immediately. Check:
1. The executable works when run normally
2. The arguments are correct
3. Any required dependencies are available

### Resource Limits Not Working

Ensure:
1. You're running as Administrator
2. The limits are reasonable (not too low)
3. Windows Job Objects are supported on your system

## Notes

- Container IDs are shown in short form (first 8 characters) in list output
- Use the full container ID for stop, inspect, and remove commands
- Containers persist in the list after stopping until explicitly removed
- Job Objects automatically clean up child processes when the main process exits
- This tool provides process-level isolation, not full OS-level containerization
