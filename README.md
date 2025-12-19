# Xcaciv.Isolation

A self-contained .NET 10 NativeAOT CLI tool for managing Windows containers using the Host Compute Service (HCS) API. Create and manage real Windows containers programmatically without dependencies on Docker or Kubernetes.

## Overview

Xcaciv.Isolation provides direct access to Windows container capabilities through the Host Compute Service API. It offers a simple command-line interface for managing Windows containers with full filesystem and process isolation.

## Features

- **Real Windows Containers**: Uses HCS API for true container isolation
- **Filesystem Isolation**: Containerized filesystem with layer support
- **Resource Management**: Control CPU and memory limits for containers
- **NativeAOT**: Self-contained executable with no .NET runtime dependency
- **Simple CLI**: Easy-to-use command-line interface powered by Spectre.Console
- **No External Dependencies**: Direct HCS API integration, no Docker or Kubernetes required

## Architecture

The project is organized into two main components:

- **Xcaciv.Isolation.Core**: Core library containing container management logic
  - Container models and interfaces
  - HCS API P/Invoke integration
  - Container lifecycle management
  
- **Xcaciv.Isolation.CLI**: Command-line interface application
  - Rich terminal UI with Spectre.Console
  - Commands for container operations
  - NativeAOT compilation support

## Requirements

- Windows 10/11 Pro/Enterprise with Containers feature enabled, or Windows Server 2016+
- .NET 10 SDK (for building)
- Administrator privileges (for container operations)
- Windows Container feature must be enabled

### Enabling Windows Containers

On Windows 10/11:
```powershell
# Run in elevated PowerShell
Enable-WindowsOptionalFeature -Online -FeatureName Containers -All
# Restart required
```

On Windows Server:
```powershell
Install-WindowsFeature -Name Containers
# Restart required
```

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

### Logging

View container logs:
```powershell
.\Xcaciv.Isolation.CLI.exe logs <container-id>
```

or using the alias:

```powershell
.\Xcaciv.Isolation.CLI.exe logs <container-id> --follow
```

**Logging Options:**
- `--follow, -f`: Follow log output (live streaming)
- `--tail <n>`: Show last N lines

### Image Management

Import an image from local directories:

```powershell
.\Xcaciv.Isolation.CLI.exe image import --name myapp --tag v1.0 --base C:\Images\Base --layer C:\Images\Layer1
```

List all images:

```powershell
.\Xcaciv.Isolation.CLI.exe image list
```

Inspect an image:

```powershell
.\Xcaciv.Isolation.CLI.exe image inspect myapp:v1.0
```

Remove an image:

```powershell
.\Xcaciv.Isolation.CLI.exe image remove myapp:v1.0
```

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

### logs

View container logs.

**Arguments:**
- `container-id`: ID of the container

**Options:**
- `--follow, -f`: Follow log output (streaming)
- `--tail <n>`: Number of lines to show from the end

### image list (ls)

List all container images.

### image import

Import a container image from local directories.

**Options:**
- `--name` (required): Name for the image
- `--tag`: Tag for the image (default: latest)
- `--base` (required): Path to the base image directory
- `--layer`: Layer directory paths (can be specified multiple times)

### image inspect

Display detailed information about an image.

**Arguments:**
- `image`: Image name with optional tag (name:tag)

### image remove (rm)

Remove a container image.

**Arguments:**
- `image`: Image name with optional tag (name:tag)

## How It Works

Xcaciv.Isolation uses the Windows Host Compute Service (HCS) API to provide true container isolation:

1. **Container Creation**: User provides executable path and configuration
2. **HCS Configuration**: System generates HCS JSON schema with container settings
3. **Compute System**: HCS creates an isolated compute system with virtual filesystem
4. **Container Start**: Container is started with specified resource limits
5. **Monitoring**: System tracks container state through HCS APIs
6. **Resource Limits**: CPU and memory limits are enforced at the hypervisor level

## Security Considerations

- Containers provide full OS-level isolation through HCS
- Filesystem is isolated from the host system
- Process trees are fully contained
- Network can be isolated (configuration dependent)
- Suitable for both development and production workloads
- Stronger isolation than process-only containers (Job Objects)
- For maximum security, consider Hyper-V isolation mode

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please follow the coding conventions outlined in `.github/copilot-instructions.md`.
