﻿using System;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Represents the parameters required for configuring and executing a disk imaging operation.
    /// </summary>
    /// <remarks>This structure encapsulates all the necessary information for performing a disk imaging
    /// process, including source and destination drives, image file details, and additional configuration
    /// options.</remarks>
    /// <param name="DestinationDrive">Gets or sets the destination drive where the image will be applied.</param>
    /// <param name="EfiDrive">Gets or sets the EFI (Extensible Firmware Interface) drive used during the imaging process.</param>
    /// <param name="DiskNumber">Gets or sets the disk number associated with the operation.</param>
    /// <param name="SourceDrive">Gets or sets the source drive containing the data to be imaged.</param>
    /// <param name="ImageIndex">Gets or sets the index of the image within the image file to be applied.</param>
    /// <param name="ImageFilePath">Gets or sets the file path of the image file to be used in the operation.</param>
    /// <param name="InstallExtraDrivers">Gets or sets a value indicating whether additional drivers should be installed during the imaging process.</param>
    /// <param name="FirmwareType">Gets or sets the firmware type of the system being imaged.</param>
    [SupportedOSPlatform("windows")]
    public struct Parameters(string DestinationDrive,
                             string EfiDrive,
                             int DiskNumber,
                             string SourceDrive,
                             int ImageIndex,
                             string ImageFilePath,
                             bool InstallExtraDrivers,
                             string FirmwareType)
    {
        public string DestinationDrive { get; set; } = DestinationDrive;
        public string EfiDrive { get; set; } = EfiDrive;
        public int DiskNumber { get; set; } = DiskNumber;
        public string SourceDrive { get; set; } = SourceDrive;
        public int ImageIndex { get; set; } = ImageIndex;
        public string ImageFilePath { get; set; } = ImageFilePath;
        public bool InstallExtraDrivers { get; set; } = InstallExtraDrivers;
        public string FirmwareType { get; set; } = FirmwareType;
    }

    /// <summary>
    /// Provides functionality to configure and install Windows on a specified disk.
    /// </summary>
    /// <remarks>This class is designed for use on Windows platforms and provides methods to set up the
    /// environment and deploy Windows installations. It requires valid parameters to be provided for successful
    /// operation.</remarks>
    [SupportedOSPlatform("windows")]
    public sealed class InstallerManager
    {
        /// <summary>
        /// Configures the specified <see cref="Parameters"/> object by prompting the user for missing values.
        /// </summary>
        /// <remarks>This method ensures that all required properties of the <paramref name="parameters"/>
        /// object are populated. If any property is missing or invalid, the user is prompted to provide the necessary
        /// input.  The method validates user input and throws exceptions for invalid or incomplete data.</remarks>
        /// <param name="parameters">A reference to the <see cref="Parameters"/> object to configure.  This object must not be null, and its
        /// properties will be updated based on user input.</param>
        /// <exception cref="ArgumentException">Thrown if the user provides invalid input, such as an improperly formatted drive letter or missing required
        /// values.</exception>
        /// <exception cref="InvalidDataException">Thrown if the firmware type cannot be determined or is invalid.</exception>
        public static void Configure(ref Parameters parameters)
        {
            #region DestinationDrive
            if (string.IsNullOrEmpty(parameters.DestinationDrive) ||
                string.IsNullOrWhiteSpace(parameters.DestinationDrive))
            {
                string p_DestinationDrive;

                Console.Write("\n==> Type the mountpoint to use for deploying Windows (e.g. Z:): ");
                try
                {
                    p_DestinationDrive = Console.ReadLine() ?? throw new ArgumentNullException(nameof(parameters), "DestinationDrive is null!");
                }
                catch (IOException)
                {
                    throw;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }

                ArgumentException.ThrowIfNullOrWhiteSpace(p_DestinationDrive);

                if (p_DestinationDrive.StartsWith(':'))
                {
                    throw new ArgumentException(@$"Invalid source drive {p_DestinationDrive}, it must have a colon at the end not at the beginning. For example: 'Z:'.");
                }
                else if (!p_DestinationDrive.EndsWith(':'))
                {
                    throw new ArgumentException($"Invalid source drive {p_DestinationDrive}, it must have a colon. For example: 'Z:'.");
                }

                parameters.DestinationDrive = p_DestinationDrive;
            }
            #endregion

            #region EfiDrive
            if (string.IsNullOrEmpty(parameters.EfiDrive) ||
                string.IsNullOrWhiteSpace(parameters.EfiDrive))
            {
                string p_EfiDrive;

                Console.Write("\n==> Type the mountpoint to use for the bootloader (e.g. Y:): ");

                try
                {
                    p_EfiDrive = Console.ReadLine() ?? throw new ArgumentNullException(nameof(parameters), "EfiDrive is null!"); ;
                }
                catch (IOException)
                {
                    throw;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }

                ArgumentException.ThrowIfNullOrWhiteSpace(p_EfiDrive);

                if (p_EfiDrive.StartsWith(':'))
                {
                    throw new ArgumentException(@$"Invalid EFI drive {p_EfiDrive}, it must have a colon at the end not at the beginning. For example: 'Y:'.");
                }
                else if (!p_EfiDrive.EndsWith(':'))
                {
                    throw new ArgumentException($"Invalid EFI drive {p_EfiDrive}, it must have a colon. For example: 'Y:'.");
                }

                parameters.EfiDrive = p_EfiDrive;
            }
            #endregion

            #region DiskNumber
            if (string.IsNullOrEmpty(parameters.DiskNumber.ToString()) ||
                string.IsNullOrWhiteSpace(parameters.DiskNumber.ToString()) ||
                parameters.DiskNumber == -1)
            {
                int p_DiskNumber;

                try
                {
                    Console.WriteLine("\n==> These are the disks available on your system:");
                    DiskManager.ListAll();
                }
                catch (Exception)
                {
                    throw;
                }

                Console.Write("\n==> Please type the disk number to format (e.g. 0): ");
                try
                {
                    p_DiskNumber = Convert.ToInt32(Console.ReadLine(), CultureInfo.CurrentCulture);
                }
                catch (FormatException)
                {
                    throw;
                }
                catch (OverflowException)
                {
                    throw;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }

                parameters.DiskNumber = p_DiskNumber;
            }
            #endregion

            #region SourceDrive
            if (string.IsNullOrEmpty(parameters.SourceDrive) ||
                string.IsNullOrWhiteSpace(parameters.SourceDrive))
            {
                string? p_SourceDrive;

                Console.Write("\n==> Specify the mountpount where the source are mounted at (e.g. X:): ");
                try
                {
                    p_SourceDrive = Console.ReadLine();
                }
                catch (IOException)
                {
                    throw;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }

                ArgumentException.ThrowIfNullOrWhiteSpace(p_SourceDrive);

                if (p_SourceDrive.StartsWith(':'))
                {
                    throw new ArgumentException(@$"Invalid source drive {p_SourceDrive}, it must have a colon at the end not at the beginning. For example: 'H:'.");
                }
                else if (!p_SourceDrive.EndsWith(':'))
                {
                    throw new ArgumentException($"Invalid source drive {p_SourceDrive}, it must have a colon. For example: 'H:'.");
                }

                parameters.SourceDrive = p_SourceDrive;
            }
            #endregion

            #region ImageFilePath
            if (string.IsNullOrEmpty(parameters.ImageFilePath) ||
                string.IsNullOrWhiteSpace(parameters.ImageFilePath))
            {
                string p_ImageFilePath = DeployManager.GetImageFile(ref parameters);

                Console.WriteLine($"\nImage file path has been set to {p_ImageFilePath}.");

                parameters.ImageFilePath = p_ImageFilePath;
            }
            #endregion

            #region ImageIndex
            if (string.IsNullOrEmpty(parameters.ImageIndex.ToString()) ||
                string.IsNullOrWhiteSpace(parameters.ImageIndex.ToString()) ||
                parameters.ImageIndex == -1)
            {
                DeployManager.GetImageInfo(ref parameters);

                Console.Write("\n==> Type the index number of the Windows edition you wish to install (e.g. 1): ");
                string? SelectedIndex = Console.ReadLine();

                if (string.IsNullOrEmpty(SelectedIndex) || string.IsNullOrWhiteSpace(SelectedIndex))
                {
                    throw new ArgumentException("No Windows edition was specified.");
                }

                parameters.ImageIndex = Convert.ToInt32(SelectedIndex, CultureInfo.CurrentCulture);
            }
            #endregion

            #region FirmwareType
            if (string.IsNullOrEmpty(parameters.FirmwareType) ||
                string.IsNullOrWhiteSpace(parameters.FirmwareType))
            {
                switch (SystemInfoManager.IsEFI())
                {
                    case true:
                        parameters.FirmwareType = "UEFI";
                        Console.WriteLine($"\nThe installer has set the firmware type to {parameters.FirmwareType}.", ConsoleColor.Yellow);
                        break;
                    case false:
                        parameters.FirmwareType = "BIOS";
                        Console.WriteLine($"\nThe installer has set the firmware type to {parameters.FirmwareType}.", ConsoleColor.Yellow);
                        break;
                    default:
                        throw new InvalidDataException($"Invalid firmware type: {parameters.FirmwareType}");
                }
            }
            #endregion
        }

        /// <summary>
        /// Installs Windows on the specified disk and configures the bootloader.
        /// </summary>
        /// <remarks>This method performs the following steps: 1. Formats the specified disk. 2. Applies
        /// the Windows image to the destination drive. 3. Installs the bootloader on the EFI drive.  Each step relies
        /// on the values provided in the <paramref name="parameters"/> object. Ensure all required properties are
        /// correctly set before calling this method. If any step fails, the corresponding exception will propagate to
        /// the caller.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object containing the necessary configuration for the
        /// installation. This includes the disk number, EFI drive, destination drive, image file path, image index, and
        /// firmware type.</param>
        /// <exception cref="InvalidDataException">Thrown if the <paramref name="parameters"/> object contains invalid or missing values, such as: - Disk
        /// number is -1. - EFI drive is null, empty, or whitespace.</exception>
        [SupportedOSPlatform("windows")]
        public static void InstallWindows(ref Parameters parameters)
        {
            if (parameters.DiskNumber.Equals(-1))
            {
                throw new InvalidDataException("No disk number was specified, required to know where to install Windows at.");
            }

            if (string.IsNullOrWhiteSpace(parameters.EfiDrive))
            {
                throw new InvalidDataException("No EFI drive was specified, required for the bootloader installation.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.DestinationDrive);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ImageFilePath);
            ArgumentOutOfRangeException.ThrowIfEqual(parameters.ImageIndex, -1);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FirmwareType);

            try
            {
                DiskManager.FormatDisk(ref parameters);
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                DeployManager.ApplyImage(ref parameters);
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                DeployManager.InstallBootloader(ref parameters);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
