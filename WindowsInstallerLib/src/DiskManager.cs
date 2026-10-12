using System;
using System.Collections.Generic;
using System.Globalization;
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
    internal sealed class DiskManager
    {
        /// <summary>
        /// Formats the disk with the specified parameters.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal static int FormatDisk(ref Parameters parameters)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(parameters.DiskNumber, nameof(parameters.DiskNumber));
            ValidateDriveLetter(parameters.EfiDrive, nameof(parameters.EfiDrive));
            ValidateDriveLetter(parameters.DestinationDrive, nameof(parameters.DestinationDrive));
            if (string.Equals(parameters.EfiDrive, parameters.DestinationDrive, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The Windows and boot partitions must use different drive letters.");
            }

            if (!GetDisks().ContainsKey(parameters.DiskNumber))
            {
                throw new InvalidDataException($"Disk {parameters.DiskNumber} is no longer available.");
            }

            if (GetProtectedDiskNumbers().Contains(parameters.DiskNumber))
            {
                throw new InvalidOperationException(
                    $"Disk {parameters.DiskNumber} contains the running Windows or boot partition and cannot be erased.");
            }

            EnsureInstallationInputsAreNotOnDisk(parameters);

            if (!InstallerManager.IsDriveLetterAvailable(parameters.EfiDrive) ||
                !InstallerManager.IsDriveLetterAvailable(parameters.DestinationDrive))
            {
                throw new InvalidOperationException("A selected installation drive letter is already in use.");
            }

            switch (PrivilegesManager.IsAdmin())
            {
                case true:
                    int exitCode = ProcessManager.StartDiskPartProcess(
                        parameters.DiskNumber,
                        parameters.EfiDrive,
                        parameters.DestinationDrive,
                        parameters.FirmwareType);
                    if (exitCode != 0)
                    {
                        throw new IOException($"DiskPart failed with exit code {exitCode}.");
                    }

                    if (GetDiskNumberForDriveLetter(parameters.EfiDrive) != parameters.DiskNumber ||
                        GetDiskNumberForDriveLetter(parameters.DestinationDrive) != parameters.DiskNumber)
                    {
                        throw new IOException("DiskPart did not create both installation volumes on the selected disk.");
                    }

                    return ProcessManager.ExitCode;

                case false:
                    throw new UnauthorizedAccessException($"You do not have enough privileges to format the disk {parameters.DiskNumber}.");
                default:
                    throw new InvalidOperationException("Could not determine whether the process has administrator privileges.");
            }
        }

        private static void ValidateDriveLetter(string driveLetter, string parameterName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(driveLetter, parameterName);
            if (driveLetter.Length != 2 ||
                !char.IsAsciiLetter(driveLetter[0]) ||
                driveLetter[1] != ':')
            {
                throw new ArgumentException("A drive letter must be in the format W:.", parameterName);
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

                    if (!string.IsNullOrWhiteSpace(Model))
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

        internal static HashSet<int> GetProtectedDiskNumbers()
        {
            HashSet<int> protectedDisks = [];
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? string.Empty;
            WqlObjectQuery query = new("SELECT * FROM Win32_DiskDrive");
            using ManagementObjectSearcher searcher = new(query);
            using ManagementObjectCollection disks = searcher.Get();

            foreach (ManagementObject disk in disks)
            {
                using (disk)
                {
                    int diskNumber = Convert.ToInt32(disk["Index"], CultureInfo.InvariantCulture);
                    using ManagementObjectCollection partitions = disk.GetRelated("Win32_DiskPartition");

                    foreach (ManagementObject partition in partitions)
                    {
                        using (partition)
                        {
                            bool isBootPartition = partition["BootPartition"] is bool bootPartition && bootPartition;
                            using ManagementObjectCollection logicalDisks = partition.GetRelated("Win32_LogicalDisk");

                            foreach (ManagementObject logicalDisk in logicalDisks)
                            {
                                using (logicalDisk)
                                {
                                    string? driveLetter = logicalDisk["DeviceID"] as string;
                                    if (isBootPartition ||
                                        string.Equals(driveLetter, systemDrive, StringComparison.OrdinalIgnoreCase))
                                    {
                                        protectedDisks.Add(diskNumber);
                                    }
                                }
                            }

                            if (isBootPartition)
                            {
                                protectedDisks.Add(diskNumber);
                            }
                        }
                    }
                }
            }

            return protectedDisks;
        }

        internal static void EnsureInstallationInputsAreNotOnDisk(Parameters parameters)
        {
            string[] inputPaths =
            [
                parameters.SourceDrive,
                parameters.ImageFilePath,
                parameters.AdditionalDrive,
                parameters.SourceMediaPath
            ];

            foreach (string path in inputPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                string? root = Path.GetPathRoot(Path.GetFullPath(path));
                if (root is null || root.Length < 2 || root[1] != ':')
                {
                    continue;
                }

                int? sourceDiskNumber = GetDiskNumberForDriveLetter(root[..2]);
                if (sourceDiskNumber == parameters.DiskNumber)
                {
                    throw new InvalidOperationException(
                        $"The selected disk also contains an installation input ({path}) and cannot be erased. " +
                        "Move the image and optional drivers to another disk before continuing.");
                }
            }
        }

        private static int? GetDiskNumberForDriveLetter(string driveLetter)
        {
            WqlObjectQuery query = new("SELECT * FROM Win32_DiskDrive");
            using ManagementObjectSearcher searcher = new(query);
            using ManagementObjectCollection disks = searcher.Get();

            foreach (ManagementObject disk in disks)
            {
                using (disk)
                {
                    int diskNumber = Convert.ToInt32(disk["Index"], CultureInfo.InvariantCulture);
                    using ManagementObjectCollection partitions = disk.GetRelated("Win32_DiskPartition");

                    foreach (ManagementObject partition in partitions)
                    {
                        using (partition)
                        using (ManagementObjectCollection logicalDisks = partition.GetRelated("Win32_LogicalDisk"))
                        {
                            foreach (ManagementObject logicalDisk in logicalDisks)
                            {
                                using (logicalDisk)
                                {
                                    if (string.Equals(
                                            logicalDisk["DeviceID"] as string,
                                            driveLetter,
                                            StringComparison.OrdinalIgnoreCase))
                                    {
                                        return diskNumber;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
