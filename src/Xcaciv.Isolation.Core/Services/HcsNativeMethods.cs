using System.Runtime.InteropServices;

namespace Xcaciv.Isolation.Core.Services;

/// <summary>
/// Native methods for Windows Host Compute Service (HCS) API
/// </summary>
internal static class HcsNativeMethods
{
    private const string HcsLibrary = "computecore.dll";

    // HCS Operation Flags
    internal const uint HCS_CREATE_OPTIONS_NONE = 0;

    // HCS Notification Types
    internal enum HCS_NOTIFICATION_TYPE
    {
        HcsNotificationSystemExited = 0,
        HcsNotificationSystemCreateCompleted = 1,
        HcsNotificationSystemStartCompleted = 2,
        HcsNotificationSystemPauseCompleted = 3,
        HcsNotificationSystemResumeCompleted = 4,
        HcsNotificationSystemCrashReport = 5,
        HcsNotificationSystemSiloJobCreated = 6,
        HcsNotificationSystemSaveCompleted = 7,
        HcsNotificationSystemRdpEnhancedModeStateChanged = 8,
        HcsNotificationSystemShutdownFailed = 9,
        HcsNotificationSystemShutdownCompleted = 10,
        HcsNotificationSystemGetPropertiesCompleted = 11,
        HcsNotificationSystemModifyCompleted = 12,
        HcsNotificationSystemCrashInitiated = 13,
        HcsNotificationSystemGuestConnectionClosed = 14,
        HcsNotificationSystemOperationCompletion = 15,
        HcsNotificationProcessExited = 0x10000,
        HcsNotificationInvalid = -1,
        HcsNotificationServiceDisconnect = 0x1000000
    }

    // HCS Operation Types
    internal enum HCS_OPERATION_TYPE
    {
        HcsOperationTypeNone = -1,
        HcsOperationTypeEnumerate = 0,
        HcsOperationTypeCreate = 1,
        HcsOperationTypeStart = 2,
        HcsOperationTypeShutdown = 3,
        HcsOperationTypePause = 4,
        HcsOperationTypeResume = 5,
        HcsOperationTypeSave = 6,
        HcsOperationTypeTerminate = 7,
        HcsOperationTypeModify = 8,
        HcsOperationTypeGetProperties = 9,
        HcsOperationTypeCrash = 10
    }

    [DllImport(HcsLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern long HcsEnumerateComputeSystems(
        [MarshalAs(UnmanagedType.LPWStr)] string? query,
        IntPtr operation);

    [DllImport(HcsLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern long HcsCreateComputeSystem(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        [MarshalAs(UnmanagedType.LPWStr)] string configuration,
        IntPtr operation,
        IntPtr securityDescriptor,
        out IntPtr computeSystem);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsStartComputeSystem(
        IntPtr computeSystem,
        IntPtr operation,
        [MarshalAs(UnmanagedType.LPWStr)] string? options);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsShutDownComputeSystem(
        IntPtr computeSystem,
        IntPtr operation,
        [MarshalAs(UnmanagedType.LPWStr)] string? options);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsTerminateComputeSystem(
        IntPtr computeSystem,
        IntPtr operation,
        [MarshalAs(UnmanagedType.LPWStr)] string? options);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsGetComputeSystemProperties(
        IntPtr computeSystem,
        IntPtr operation,
        [MarshalAs(UnmanagedType.LPWStr)] string? propertyQuery);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsCloseComputeSystem(IntPtr computeSystem);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern IntPtr HcsCreateOperation(
        IntPtr context,
        HcsOperationCallback callback);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern void HcsCloseOperation(IntPtr operation);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern long HcsGetOperationResult(
        IntPtr operation,
        out IntPtr resultDocument);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern IntPtr HcsGetProcessInfo(
        IntPtr process,
        IntPtr operation);

    [DllImport(HcsLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern long HcsGetComputeSystemFromId(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        out IntPtr computeSystem);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate void HcsOperationCallback(
        IntPtr operation,
        IntPtr context);

    [DllImport(HcsLibrary, SetLastError = true)]
    internal static extern void HcsFreeMemory(IntPtr buffer);

    // Helper to check for success
    internal static bool Succeeded(long hresult)
    {
        return hresult >= 0;
    }

    // Helper to get error message
    internal static string GetErrorMessage(long hresult)
    {
        return $"HCS Error: 0x{hresult:X8}";
    }
}
