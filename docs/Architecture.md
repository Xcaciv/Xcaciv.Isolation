# Architecture Documentation

## Overview

Xcaciv.Isolation is a Windows container runtime tool built on .NET 10 with NativeAOT compilation. It provides lightweight process isolation and resource management using Windows Job Objects.

## System Architecture

```
┌─────────────────────────────────────────────┐
│         Xcaciv.Isolation.CLI                │
│  (Command-Line Interface - NativeAOT)       │
│                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐   │
│  │  Start   │ │  List    │ │  Stop    │   │
│  │ Command  │ │ Command  │ │ Command  │   │
│  └──────────┘ └──────────┘ └──────────┘   │
│  ┌──────────┐ ┌──────────┐                │
│  │ Inspect  │ │  Remove  │                │
│  │ Command  │ │ Command  │                │
│  └──────────┘ └──────────┘                │
└─────────────────┬───────────────────────────┘
                  │
                  │ IContainerManager
                  │
┌─────────────────▼───────────────────────────┐
│        Xcaciv.Isolation.Core                │
│  (Core Container Management Library)        │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │  WindowsContainerManager            │   │
│  │  - StartAsync()                     │   │
│  │  - StopAsync()                      │   │
│  │  - ListContainersAsync()            │   │
│  │  - GetContainerAsync()              │   │
│  │  - RemoveAsync()                    │   │
│  └─────────────┬───────────────────────┘   │
│                │                            │
│  ┌─────────────▼───────────────────────┐   │
│  │  WindowsJobObject                   │   │
│  │  - SetMemoryLimit()                 │   │
│  │  - SetCpuLimit()                    │   │
│  │  - AssignProcess()                  │   │
│  └─────────────┬───────────────────────┘   │
│                │                            │
│  ┌─────────────▼───────────────────────┐   │
│  │  NativeMethods                      │   │
│  │  - P/Invoke to Win32 APIs          │   │
│  │  - Job Object Operations            │   │
│  │  - Process Management               │   │
│  └─────────────────────────────────────┘   │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│          Windows Operating System           │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │     Windows Job Objects API          │  │
│  │  - Process Isolation                 │  │
│  │  - Resource Limits                   │  │
│  │  - Lifecycle Management              │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

## Component Details

### Xcaciv.Isolation.CLI

The command-line interface layer that provides user-facing commands.

**Key Components:**
- **Program.cs**: Entry point and command router
- **Commands/**: Individual command implementations
  - StartCommand: Creates and starts new containers
  - StopCommand: Stops running containers
  - ListCommand: Lists all containers
  - InspectCommand: Shows detailed container information
  - RemoveCommand: Removes stopped containers

**Technologies:**
- System.CommandLine: Command-line parsing
- Spectre.Console: Rich terminal UI
- NativeAOT: Ahead-of-time compilation

### Xcaciv.Isolation.Core

The core library that implements container management logic.

**Key Components:**

#### Models
- **ContainerState**: Enumeration of container states (Running, Stopped, Paused, Unknown)
- **ContainerInfo**: Container metadata and status information
- **ContainerConfiguration**: Configuration for creating containers

#### Interfaces
- **IContainerManager**: Contract for container management operations

#### Services
- **WindowsContainerManager**: Main implementation of IContainerManager
  - Manages container lifecycle
  - Tracks active containers
  - Monitors process exit
  - Handles cleanup

- **WindowsJobObject**: Wrapper around Windows Job Object API
  - Creates and configures job objects
  - Sets resource limits (CPU, memory)
  - Assigns processes to jobs
  - Manages job lifecycle

- **NativeMethods**: P/Invoke declarations for Win32 APIs
  - Job Object APIs (CreateJobObject, SetInformationJobObject)
  - Process APIs (OpenProcess, TerminateProcess)
  - Native structures and constants

#### Exceptions
- **ContainerException**: Base exception for container operations
- **ContainerNotFoundException**: Thrown when container is not found
- **ContainerConfigurationException**: Thrown for invalid configuration

## Windows Job Objects

Windows Job Objects are the foundation of the container isolation mechanism.

### Key Features

1. **Process Grouping**: Multiple processes can be assigned to a job
2. **Resource Limits**: 
   - Memory limits (per-process and per-job)
   - CPU rate limits (percentage-based)
3. **Lifecycle Management**: 
   - JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE ensures cleanup
   - All processes in job terminate when job is closed
4. **Monitoring**: Ability to query job resource usage

### Limitations

- Not as isolated as Docker containers or Hyper-V containers
- Processes in a job can still interact with the host system
- No filesystem isolation
- No network isolation
- Suitable for development/testing, not production isolation

## Data Flow

### Starting a Container

1. User executes: `start --name myapp --exec app.exe`
2. CLI parses command and options
3. StartCommand creates ContainerConfiguration
4. Calls WindowsContainerManager.StartAsync()
5. Manager validates configuration
6. Creates WindowsJobObject with unique name
7. Applies resource limits to job object
8. Starts process using Process.Start()
9. Assigns process to job object
10. Registers container in tracking dictionary
11. Returns ContainerInfo to CLI
12. CLI displays success message with container details

### Stopping a Container

1. User executes: `stop <container-id>`
2. CLI parses command
3. Calls WindowsContainerManager.StopAsync()
4. Manager looks up container by ID
5. Calls Process.Kill() on container process
6. Job object automatically cleans up all child processes
7. Container remains in list with Stopped state
8. Returns success to CLI

### Listing Containers

1. User executes: `list`
2. CLI calls WindowsContainerManager.ListContainersAsync()
3. Manager iterates tracked containers
4. Checks process status for each
5. Returns collection of ContainerInfo
6. CLI formats and displays as table

## Security Model

### Trust Boundaries

1. **User Input → CLI**: 
   - Command-line arguments are validated
   - File paths are checked for existence
   - Numeric values are range-checked

2. **CLI → Core**:
   - Configuration objects are validated
   - Invalid configurations throw exceptions
   - Resource limits are enforced

3. **Core → Windows**:
   - P/Invoke calls use safe handles
   - Error codes are checked
   - Resources are properly disposed

### Isolation Level

The current implementation provides **process-level isolation** through Job Objects:

- ✅ Process lifetime management
- ✅ Resource limits (CPU, memory)
- ✅ Automatic cleanup on termination
- ❌ No filesystem isolation
- ❌ No network isolation
- ❌ Processes can interact with host

For stronger isolation, consider:
- Windows Server Containers (with containerd)
- Hyper-V Containers (VM-based isolation)
- Docker Desktop for Windows

## NativeAOT Compilation

The CLI is compiled with NativeAOT for:

1. **Self-contained deployment**: No .NET runtime required
2. **Fast startup**: No JIT compilation at runtime
3. **Smaller memory footprint**: Reduced overhead
4. **Better trimming**: Only required code included

### Build Configuration

The "Compact" configuration enables:
- `PublishAot=true`: AOT compilation
- `PublishTrimmed=true`: Remove unused code
- `PublishSingleFile=true`: Single executable
- `SelfContained=true`: Include runtime
- `TrimmerDefaultAction=link`: Aggressive trimming

### AOT Considerations

- Reflection is limited (types must be statically referenced)
- Dynamic code generation is not supported
- Some libraries may not be AOT-compatible
- Increased build time

## Extension Points

The architecture supports future enhancements:

1. **Additional Isolation Mechanisms**:
   - Windows Server Container integration
   - Namespace isolation
   - Filesystem layering

2. **Enhanced Monitoring**:
   - Real-time resource usage
   - Performance metrics
   - Log aggregation

3. **Networking**:
   - Virtual networking
   - Port mapping
   - Network isolation

4. **Storage**:
   - Volume mounts
   - Filesystem overlay
   - Persistent storage

5. **Orchestration**:
   - Multi-container deployments
   - Service discovery
   - Health checks

## Performance Characteristics

### Memory Usage
- Base CLI: ~5-10 MB (NativeAOT)
- Per container: ~1-2 MB overhead (job object + tracking)
- Container process: Depends on executable

### Startup Time
- CLI startup: <100ms (NativeAOT)
- Container start: <500ms (process creation + job assignment)

### Resource Overhead
- Minimal CPU overhead for container management
- Job objects add negligible overhead to process execution
- Memory limits enforced by Windows kernel

## Future Considerations

1. **Persistence**: 
   - Save container state to disk
   - Restart containers after reboot
   - Container logs

2. **Advanced Features**:
   - Container images
   - Layered filesystems
   - Network namespaces

3. **Integration**:
   - OCI runtime specification compatibility
   - containerd integration
   - Windows Container runtime interop

4. **Monitoring**:
   - Metrics endpoint
   - Health checks
   - Event logging
