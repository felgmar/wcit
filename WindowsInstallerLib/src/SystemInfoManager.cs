using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Provides methods for retrieving system firmware information and determining the firmware type.
    /// </summary>
    /// <remarks>This class includes functionality to check if the system is using EFI (Extensible Firmware
    /// Interface) firmware. It is supported only on Windows platforms.</remarks>
    [SupportedOSPlatform("windows")]
    internal static partial class SystemInfoManager
    {
        /// <summary>
        /// Retrieves the firmware type of the system by querying a firmware environment variable.
        /// </summary>
        /// <remarks>This method is a platform invocation (P/Invoke) wrapper for the native Windows API
        /// function  <c>GetFirmwareEnvironmentVariableA</c>. It allows managed code to interact with firmware
        /// environment variables.</remarks>
        /// <param name="lpName">The name of the firmware environment variable to query.</param>
        /// <param name="lpGUID">The globally unique identifier (GUID) of the firmware environment variable namespace.</param>
        /// <param name="pBuffer">A pointer to a buffer that receives the value of the firmware environment variable.</param>
        /// <param name="size">The size, in bytes, of the buffer pointed to by <paramref name="pBuffer"/>.</param>
        /// <returns>The size of the data retrieved, in bytes, if the call succeeds; otherwise, 0 if the call fails.</returns>
        [LibraryImport("kernel32.dll", EntryPoint = "GetFirmwareEnvironmentVariableA", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static partial uint GetFirmwareType(string lpName, string lpGUID, IntPtr pBuffer, uint size);

        /// <summary>
        /// Determines whether the system supports EFI (Extensible Firmware Interface).
        /// </summary>
        /// <remarks>This method checks the system's firmware type by invoking a low-level function and
        /// analyzing the result. It relies on the last Win32 error code to determine EFI support.</remarks>
        /// <returns><see langword="true"/> if the system supports EFI; otherwise, <see langword="false"/>.</returns>
        internal static bool IsEFI()
        {
            // Call the function with a dummy variable name and a dummy variable namespace (function will fail because these don't exist.)
            GetFirmwareType("DummyVariableName", "{00000000-0000-0000-0000-000000000000}", IntPtr.Zero, 0);

            // Check the last Win32 error to determine if the system supports EFI
            return Marshal.GetLastWin32Error() != 1;
        }
    }
}
