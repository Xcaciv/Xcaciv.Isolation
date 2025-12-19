using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Windows Job Object wrapper for process isolation
/// </summary>
internal sealed class WindowsJobObject : IDisposable
{
    private readonly SafeFileHandle jobHandle;
    private bool disposed;

    public WindowsJobObject(string jobName)
    {
        jobHandle = NativeMethods.CreateJobObject(IntPtr.Zero, jobName);
        
        if (jobHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create job object: {Marshal.GetLastWin32Error()}");
        }

        ConfigureJobLimits();
    }

    private void ConfigureJobLimits()
    {
        var info = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        };

        var extendedInfo = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = info
        };

        int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!NativeMethods.SetInformationJobObject(jobHandle, 
                NativeMethods.JobObjectInfoType.ExtendedLimitInformation,
                extendedInfoPtr, 
                (uint)length))
            {
                throw new InvalidOperationException($"Failed to set job limits: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    public void SetMemoryLimit(long limitBytes)
    {
        var info = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE | NativeMethods.JOB_OBJECT_LIMIT_JOB_MEMORY
        };

        var extendedInfo = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = info,
            JobMemoryLimit = (UIntPtr)limitBytes
        };

        int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
        IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

            if (!NativeMethods.SetInformationJobObject(jobHandle,
                NativeMethods.JobObjectInfoType.ExtendedLimitInformation,
                extendedInfoPtr,
                (uint)length))
            {
                throw new InvalidOperationException($"Failed to set memory limit: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(extendedInfoPtr);
        }
    }

    public void SetCpuLimit(int percentLimit)
    {
        var cpuInfo = new NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION
        {
            ControlFlags = NativeMethods.JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | NativeMethods.JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP,
            CpuRate = (uint)(percentLimit * 100) // CpuRate is in hundredths of a percent
        };

        int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_CPU_RATE_CONTROL_INFORMATION));
        IntPtr cpuInfoPtr = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(cpuInfo, cpuInfoPtr, false);

            if (!NativeMethods.SetInformationJobObject(jobHandle,
                NativeMethods.JobObjectInfoType.CpuRateControlInformation,
                cpuInfoPtr,
                (uint)length))
            {
                throw new InvalidOperationException($"Failed to set CPU limit: {Marshal.GetLastWin32Error()}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(cpuInfoPtr);
        }
    }

    public void AssignProcess(IntPtr processHandle)
    {
        if (!NativeMethods.AssignProcessToJobObject(jobHandle, processHandle))
        {
            throw new InvalidOperationException($"Failed to assign process to job: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            jobHandle?.Dispose();
            disposed = true;
        }
    }
}
