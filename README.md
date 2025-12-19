# Xcaciv.Isolation

A self-contained .NET 10 NativeAOT CLI tool that hides the plumbing and lets you start, stop, inspect, and configure Windows containers without dependency on tools like VMs, Docker or Kubernetes.

## Overview

Xcaciv.Isolation provides a lightweight Windows container runtime using Windows Job Objects for process isolation and resource management. It offers a simple command-line interface for managing containerized Windows applications directly on Windows 10/11 and Windows Server.

## Features

- **Lightweight Isolation**: Uses Windows Job Objects for process isolation without VM overhead
- **Resource Management**: Control CPU and memory limits for containers
- **NativeAOT**: Self-contained executable with no .NET runtime dependency
- **Simple CLI**: Easy-to-use command-line interface powered by Spectre.Console
- **No External Dependencies**: No Docker, Kubernetes, or VM requirements

## Architecture

The project is organized into two main components:

- **Xcaciv.Isolation.Core**: Core library containing container management logic
  - Container models and interfaces
  - Windows Job Object integration
  - Container lifecycle management
  
- **Xcaciv.Isolation.CLI**: Command-line interface application
  - Rich terminal UI with Spectre.Console
  - Commands for container operations
  - NativeAOT compilation support

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 10 SDK (for building)
- Administrator privileges (for container operations)

## Building

### Using Visual Studio 2026

1. Open `Xcaciv.Isolation.sln`
2. Select the desired configuration (Debug, Release, or Compact)
3. Build the solution

### Build Configurations

- **Debug**: Standard debug build with full debugging symbols
- **Release**: Optimized release build
- **Compact**: NativeAOT build producing a self-contained single-file executable

The Compact configuration produces a fully self-contained executable with:
- Ahead-of-time (AOT) compilation
- Trimmed dependencies
- Single-file output
- No runtime dependency

## Usage

### Start a Container

Start a new container with an executable:

```powershell
Xcaciv.Isolation.CLI.exe start --name mycontainer --exec C:\Windows\System32\notepad.exe
```

With resource limits:

```powershell
Xcaciv.Isolation.CLI.exe start --name myapp --exec C:\MyApp\app.exe --memory 512 --cpu 50
```

With arguments and environment variables:

```powershell
Xcaciv.Isolation.CLI.exe start --name myapp --exec C:\MyApp\app.exe --args arg1 arg2 --env KEY=VALUE --env DEBUG=true
```

### List Containers

List all containers:

```powershell
Xcaciv.Isolation.CLI.exe list
```

or using the alias:

```powershell
Xcaciv.Isolation.CLI.exe ls
```

### Inspect a Container

Display detailed information about a container:

```powershell
Xcaciv.Isolation.CLI.exe inspect <container-id>
```

### Stop a Container

Stop a running container:

```powershell
Xcaciv.Isolation.CLI.exe stop <container-id>
```

### Remove a Container

Remove a stopped container:

```powershell
Xcaciv.Isolation.CLI.exe remove <container-id>
```

Force remove a running container:

```powershell
Xcaciv.Isolation.CLI.exe remove <container-id> --force
```

or using the alias:

```powershell
Xcaciv.Isolation.CLI.exe rm <container-id> -f
```

## Command Reference

### start

Start a new container.

**Options:**
- `--name` (required): Name for the container
- `--exec` (required): Path to the executable to run
- `--args`: Arguments to pass to the executable
- `--workdir`: Working directory for the container
- `--memory`: Memory limit in MB
- `--cpu`: CPU limit as percentage (1-100)
- `--env`: Environment variables in KEY=VALUE format

### list (ls)

List all containers.

### inspect

Display detailed information about a container.

**Arguments:**
- `container-id`: ID of the container to inspect

### stop

Stop a running container.

**Arguments:**
- `container-id`: ID of the container to stop

### remove (rm)

Remove a stopped container.

**Arguments:**
- `container-id`: ID of the container to remove

**Options:**
- `--force, -f`: Force remove a running container

## How It Works

Xcaciv.Isolation uses Windows Job Objects to provide process isolation and resource management:

1. **Job Objects**: Each container runs in its own Windows Job Object, which provides:
   - Process isolation
   - Resource limits (CPU, memory)
   - Automatic cleanup when the job terminates

2. **Process Management**: The container manager tracks running processes and their associated job objects

3. **Resource Controls**: CPU and memory limits are enforced through job object limits

## Security Considerations

- Containers are isolated at the process level using Windows Job Objects
- This provides lighter isolation than full containerization (Docker/Windows Containers)
- Suitable for development and testing scenarios
- For production workloads, consider Windows Server Containers or Hyper-V isolation

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please follow the coding conventions outlined in `.github/copilot-instructions.md`.
