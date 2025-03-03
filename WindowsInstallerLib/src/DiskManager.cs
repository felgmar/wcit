using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Manages the disks on the system.
    /// </summary>
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
        /// Lists all the disks on the system.
        /// </summary>
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
        /// Lists all disk on the system using DriveInfo.
        /// </summary>
        /// <returns></returns>
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
