using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Versioning;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Provides functionality for managing and interacting with system disks on Windows platforms.
    /// </summary>
    /// <remarks>This class includes methods for formatting disks, listing available disks, and retrieving
    /// disk information. It requires administrative privileges for certain operations, such as formatting a
    /// disk.</remarks>
    [SupportedOSPlatform("windows")]
    internal class DiskManager
    {
        /// <summary>
        /// Formats the disk with the specified parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal static int FormatDisk(ref Parameters parameters)
        {
            try
            {
                ArgumentException.ThrowIfNullOrEmpty(parameters.EfiDrive);
                ArgumentException.ThrowIfNullOrEmpty(parameters.DestinationDrive);

                switch (PrivilegesManager.IsAdmin())
                {
                    case true:
                        ProcessManager.StartDiskPartProcess(parameters.DiskNumber, parameters.EfiDrive, parameters.DestinationDrive);
                        return ProcessManager.ExitCode;

                    case false:
                        throw new UnauthorizedAccessException($"You do not have enough privileges to format the disk {parameters.DiskNumber}.");
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Lists all disk drives on the system and displays their details, including disk number, model, and device ID.
        /// </summary>
        /// <remarks>This method queries the system's disk drives using WMI (Windows Management
        /// Instrumentation) and outputs the retrieved information to the console. The details include the disk number,
        /// model, and device ID for each disk drive found. <para> Note that this method is intended for internal use
        /// and writes directly to the console. It does not return the retrieved data or provide a way to
        /// programmatically access it. </para></remarks>
        internal static void ListAll()
        {
            try
            {
                WqlObjectQuery DeviceTable = new("SELECT * FROM Win32_DiskDrive");
                ManagementObjectSearcher DeviceInfo = new(DeviceTable);
                foreach (ManagementObject o in DeviceInfo.Get().Cast<ManagementObject>())
                {
                    Console.WriteLine("Disk number = " + o["Index"]);
                    Console.WriteLine("Model = " + o["Model"]);
                    Console.WriteLine("DeviceID = " + o["DeviceID"]);
                    Console.WriteLine("");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves an array of all available drive information on the system.
        /// </summary>
        /// <returns>An array of <see cref="DriveInfo"/> objects representing the drives available on the system.</returns>
        internal static DriveInfo[] GetDisksT()
        {
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();

                return drives;
            }
            catch (IOException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
