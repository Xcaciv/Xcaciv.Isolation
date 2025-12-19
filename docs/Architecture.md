# Architecture Documentation

## Overview

Xcaciv.Isolation is a Windows container runtime tool built on .NET 10 with NativeAOT compilation. It provides true container isolation and resource management using the Windows Host Compute Service (HCS) API - the same low-level API that powers Docker and Windows Server Containers.

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
│  │  HcsContainerManager                │   │
│  │  - StartAsync()                     │   │
│  │  - StopAsync()                      │   │
│  │  - ListContainersAsync()            │   │
│  │  - GetContainerAsync()              │   │
│  │  - RemoveAsync()                    │   │
│  └─────────────┬───────────────────────┘   │
│                │                            │
│  ┌─────────────▼───────────────────────┐   │
│  │  HcsNativeMethods                   │   │
│  │  - P/Invoke to HCS COM APIs        │   │
│  │  - HcsCreateComputeSystem           │   │
│  │  - HcsStartComputeSystem            │   │
│  │  - HcsTerminateComputeSystem        │   │
│  │  - HcsGetComputeSystemProperties    │   │
│  └─────────────────────────────────────┘   │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│          Windows Operating System           │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │  Host Compute Service (HCS) API      │  │
│  │  - True Container Isolation          │  │
│  │  - Filesystem Isolation & Layers     │  │
│  │  - Resource Limits (Hypervisor)     │  │
│  │  - Network Isolation                 │  │
│  │  - Process Tree Containment          │  │
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

The core library that implements container management logic using HCS API.

**Key Components:**

#### Models
- **ContainerState**: Enumeration of container states (Running, Stopped, Paused, Unknown)
- **ContainerInfo**: Container metadata and status information
- **ContainerConfiguration**: Configuration for creating containers

#### Interfaces
- **IContainerManager**: Contract for container management operations

#### Services
- **HcsContainerManager**: Main implementation of IContainerManager
  - Manages container lifecycle via HCS API
  - Tracks active containers
  - Builds HCS configuration JSON
  - Handles HCS operation callbacks

- **HcsNativeMethods**: P/Invoke declarations for HCS COM APIs
  - HcsCreateComputeSystem: Create new container
  - HcsStartComputeSystem: Start container
  - HcsShutDownComputeSystem: Graceful shutdown
  - HcsTerminateComputeSystem: Force termination
  - HcsGetComputeSystemProperties: Query container state
  - HcsEnumerateComputeSystems: List all containers

#### Exceptions
- **ContainerException**: Base exception for container operations
- **ContainerNotFoundException**: Thrown when container is not found
- **ContainerConfigurationException**: Thrown for invalid configuration

## Windows Host Compute Service (HCS)

HCS is the foundation of Windows container technology and provides true OS-level containerization.

### Key Features

1. **Compute Systems**: HCS manages isolated compute systems (containers or VMs)
2. **Filesystem Isolation**: 
   - Layer-based filesystem with copy-on-write
   - Isolated from host filesystem
   - Support for mapped directories
3. **Resource Limits**: 
   - Memory limits enforced at hypervisor level
   - CPU rate control
   - Configurable virtual hardware topology
4. **Network Isolation**: 
   - Virtual network interfaces
   - Network namespace isolation
   - HvSocket for host communication
5. **Process Containment**: 
   - Complete process tree isolation
   - Automatic cleanup of all container processes

### HCS Configuration Schema

HCS uses JSON configuration (Schema v2.1) to define containers:

```json
{
  "SchemaVersion": { "Major": 2, "Minor": 1 },
  "Owner": "Xcaciv.Isolation",
  "ShouldTerminateOnLastHandleClosed": true,
  "VirtualMachine": {
    "ComputeTopology": {
      "Memory": { "SizeInMB": 2048 },
      "Processor": { "Count": 1, "Limit": 10000 }
    },
    "Devices": {
      "HvSocket": {
        "HvSocketConfig": {
          "DefaultBindSecurityDescriptor": "D:P(A;;FA;;;WD)"
        }
      }
    }
  },
  "Container": {
    "MappedDirectories": [
      {
        "HostPath": "C:\\App",
        "ContainerPath": "C:\\app",
        "ReadOnly": false
      }
    ],
    "HvPartition": true
  }
}
```

### Comparison with Job Objects

| Feature | HCS Containers | Job Objects |
|---------|---------------|-------------|
| Filesystem Isolation | ✅ Full isolation | ❌ Shared with host |
| Network Isolation | ✅ Virtual network | ❌ Shared with host |
| Process Isolation | ✅ Complete containment | ⚠️  Limited grouping |
| Resource Enforcement | ✅ Hypervisor level | ⚠️  Kernel level |
| Security | ✅ Production-ready | ⚠️  Development only |
| Overhead | ⚠️  Moderate | ✅ Minimal |

## Data Flow

### Starting a Container

1. User executes: `start --name myapp --exec app.exe --memory 512`
2. CLI parses command and options
3. StartCommand creates ContainerConfiguration
4. Calls HcsContainerManager.StartAsync()
5. Manager validates configuration
6. Builds HCS JSON configuration with resource limits
7. Calls HcsCreateComputeSystem via P/Invoke
8. HCS creates isolated compute system with:
   - Virtual filesystem
   - Network configuration
   - Resource limits
9. Calls HcsStartComputeSystem to boot container
10. Container starts with isolated environment
11. Returns ContainerInfo to CLI
12. CLI displays success message

### Stopping a Container

1. User executes: `stop <container-id>`
2. CLI parses command
3. Calls HcsContainerManager.StopAsync()
4. Manager looks up container by ID
5. Attempts graceful shutdown via HcsShutDownComputeSystem
6. If needed, force terminates via HcsTerminateComputeSystem
7. HCS cleans up all container resources
8. Returns success to CLI

### Listing Containers

1. User executes: `list`
2. CLI calls HcsContainerManager.ListContainersAsync()
3. Manager iterates tracked containers
4. Returns collection of ContainerInfo
5. CLI formats and displays as table

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

3. **Core → HCS**:
   - P/Invoke calls use COM interfaces
   - Error codes are checked from HRESULT
   - Resources are properly disposed
   - JSON configuration is serialized safely

### Isolation Level

The current implementation provides **OS-level container isolation** through HCS:

- ✅ Complete filesystem isolation
- ✅ Network isolation (configurable)
- ✅ Process tree containment
- ✅ Resource limits at hypervisor level
- ✅ Automatic cleanup of all resources
- ✅ Production-ready security

This is the same isolation level as:
- Docker for Windows (when using Windows containers)
- Windows Server Containers
- Azure Container Instances (Windows)

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
- P/Invoke to native APIs works perfectly
- Increased build time

## Extension Points

The architecture supports future enhancements:

1. **Container Images**:
   - Support for Windows container images
   - Layer management and caching
   - Image registry integration

2. **Advanced Networking**:
   - Network configuration
   - Port mapping
   - Multiple network interfaces

3. **Enhanced Monitoring**:
   - Real-time resource usage via HCS queries
   - Performance metrics
   - Event-driven status updates

4. **Storage**:
   - Volume mounts
   - Persistent storage
   - Layer optimization

5. **Orchestration**:
   - Multi-container deployments
   - Service discovery
   - Health checks

## Performance Characteristics

### Memory Usage
- Base CLI: ~5-10 MB (NativeAOT)
- Per container: Variable (depends on container workload)
- HCS overhead: Moderate (full isolation trade-off)

### Startup Time
- CLI startup: <100ms (NativeAOT)
- Container creation: 2-5 seconds (includes filesystem setup)
- Container start: <1 second

### Resource Overhead
- Minimal CPU overhead for management
- Memory limits enforced by hypervisor
- Filesystem I/O through layered driver

## Future Considerations

1. **Image Management**:
   - Pull images from registries
   - Build custom container images
   - Layer caching and optimization

2. **Advanced Features**:
   - GPU passthrough
   - USB device mapping
   - Audio/video streaming

3. **Integration**:
   - OCI runtime specification compatibility
   - Kubernetes integration via CRI
   - Docker compatibility layer

4. **Production Features**:
   - Health checks and restarts
   - Log aggregation
   - Metrics and monitoring endpoints
