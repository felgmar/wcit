using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
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

        internal static List<Tuple<int, string, string>> GetDisks()
        {
            List<Tuple<int, string, string>> DisksList = [];
            try
            {
                WqlObjectQuery DeviceTable = new("SELECT * FROM Win32_DiskDrive");
                ManagementObjectSearcher DeviceInfo = new(DeviceTable);
                foreach (ManagementObject o in DeviceInfo.Get().Cast<ManagementObject>())
                {
                    int Index = Convert.ToInt32(o["Index"]);
                    string Model = o["Model"].ToString() ?? string.Empty;
                    string Letter = GetDiskLetter(o["DeviceID"]);

                    DisksList.Add(new(Index, Model, Letter));
                }
                return DisksList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GetDiskLetter(object DeviceID)
        {
            string DriveLetter = string.Empty;

            ArgumentNullException.ThrowIfNull(DeviceID);

            try
            {
                WqlObjectQuery PartitionQuery = new($"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{DeviceID}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                ManagementObjectSearcher PartitionSearcher = new(PartitionQuery);
                foreach (ManagementObject partition in PartitionSearcher.Get().Cast<ManagementObject>())
                {
                    WqlObjectQuery LogicalDiskQuery = new($"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");
                    ManagementObjectSearcher LogicalDiskSearcher = new(LogicalDiskQuery);
                    foreach (ManagementObject logicalDisk in LogicalDiskSearcher.Get().Cast<ManagementObject>())
                    {
                        DriveLetter = logicalDisk["DeviceID"].ToString() ?? throw new ArgumentNullException("DeviceID cannot be null", DeviceID.ToString());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return DriveLetter;
        }


        /// <summary>
        /// Lists all disk drives on the system and displays their details, including disk number, model, and device ID.
        /// </summary>
        /// <remarks>This method queries the system's disk drives using WMI (Windows Management
        /// Instrumentation) and outputs the retrieved information to the console. The details include the disk number,
        /// model, and device ID for each disk drive found. <para> Note that this method is intended for internal use
        /// and writes directly to the console. It does not return the retrieved data or provide a way to
        /// programmatically access it. </para></remarks>
        internal static SortedDictionary<int, string> GetDisks()
        {
            SortedDictionary<int, string> SystemDisks = [];

            try
            {
                WqlObjectQuery Query = new("SELECT * FROM Win32_DiskDrive");
                ManagementObjectSearcher ObjectSearcher = new(Query);
                foreach (ManagementObject obj in ObjectSearcher.Get().Cast<ManagementObject>())
                {
                    int Index = Convert.ToInt32(obj["Index"], CultureInfo.CurrentCulture);
                    string? Model = obj["Model"].ToString();

                    if (Model != null)
                    {
                        SystemDisks.Add(Index, Model);
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }

            return SystemDisks;
        }
    }
}
