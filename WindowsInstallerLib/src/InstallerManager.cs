using System;
using System.Collections.Generic;
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
    /// <param name="AdditionalDrive">Gets or sets a value indicating whether additional drivers should be installed during the imaging process.</param>
    /// <param name="FirmwareType">Gets or sets the firmware type of the system being imaged.</param>
    [SupportedOSPlatform("windows")]
    public struct Parameters(string DestinationDrive,
                             string EfiDrive,
                             int DiskNumber,
                             string SourceDrive,
                             int ImageIndex,
                             string ImageFilePath,
                             string AdditionalDrive,
                             string FirmwareType)
    {
        public string DestinationDrive { get; set; } = DestinationDrive;
        public string EfiDrive { get; set; } = EfiDrive;
        public int DiskNumber { get; set; } = DiskNumber;
        public string SourceDrive { get; set; } = SourceDrive;
        public int ImageIndex { get; set; } = ImageIndex;
        public string ImageFilePath { get; set; } = ImageFilePath;
        public string AdditionalDrive { get; set; } = AdditionalDrive;
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
            if (string.IsNullOrWhiteSpace(parameters.DestinationDrive))
            {
                Console.Write("\n==> Type the mountpoint to use for deploying Windows (e.g. Z:): ");

                try
                {
                    parameters.DestinationDrive = Console.ReadLine() ??
                        throw new ArgumentException("A valid destination drive is required.", nameof(parameters));
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

                if (parameters.DestinationDrive.Length > 2 ||
                    parameters.DestinationDrive.Length < 2)
                {
                    throw new ArgumentException($"Invalid destination drive {parameters.DestinationDrive}.");
                }

                if (parameters.DestinationDrive.StartsWith(':') || !parameters.DestinationDrive.EndsWith(':'))
                {
                    throw new InvalidDataException($"Invalid source drive {parameters.DestinationDrive}.");
                }
            }
            #endregion

            #region EfiDrive
            if (string.IsNullOrWhiteSpace(parameters.EfiDrive))
            {
                Console.Write("\n==> Type the mountpoint to use for the bootloader (e.g. Y:): ");

                try
                {
                    parameters.EfiDrive = Console.ReadLine() ??
                        throw new ArgumentException("A valid EFI drive is required.", nameof(parameters));
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

                if (parameters.EfiDrive.StartsWith(':'))
                {
                    throw new ArgumentException(@$"Invalid EFI drive {parameters.EfiDrive}, it must have a colon at the end not at the beginning. For example: 'Y:'.");
                }

                if (!parameters.EfiDrive.EndsWith(':'))
                {
                    throw new ArgumentException($"Invalid EFI drive {parameters.EfiDrive}, it must have a colon. For example: 'Y:'.");
                }
            }
            #endregion

            #region DiskNumber
            if (string.IsNullOrWhiteSpace(parameters.DiskNumber.ToString()) || parameters.DiskNumber == -1)
            {
                Console.WriteLine("\n==> These are the disks available on your system:");

                try
                {
                    SortedDictionary<int, string> Disks = DiskManager.GetDisks();

                    foreach (KeyValuePair<int, string> Disk in Disks)
                    {
                        Console.WriteLine("Disk number: {0}\nDisk model: {1}\n", Disk.Key, Disk.Value);
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                Console.Write("\n==> Please type the disk number to format (e.g. 0): ");
                try
                {
                    parameters.DiskNumber = Convert.ToInt32(Console.ReadLine(), CultureInfo.CurrentCulture);
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
            }
            #endregion

            #region SourceDrive
            if (string.IsNullOrWhiteSpace(parameters.SourceDrive))
            {
                Console.Write("\n==> Specify the mount point where the source are mounted at (e.g. X:): ");

                try
                {
                    parameters.SourceDrive = Console.ReadLine() ??
                        throw new ArgumentException("A sourced drive is required.", nameof(parameters));
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

                ArgumentException.ThrowIfNullOrWhiteSpace(parameters.SourceDrive);

                if (parameters.SourceDrive.StartsWith(':'))
                {
                    throw new ArgumentException(@$"Invalid source drive {parameters.SourceDrive}, it must have a colon at the end not at the beginning. For example: 'H:'.");
                }
                else if (!parameters.SourceDrive.EndsWith(':'))
                {
                    throw new ArgumentException($"Invalid source drive {parameters.SourceDrive}, it must have a colon. For example: 'H:'.");
                }
            }
            #endregion

            #region ImageFilePath
            if (string.IsNullOrWhiteSpace(parameters.ImageFilePath))
            {
                parameters.ImageFilePath = DeployManager.GetImageFile(ref parameters);

                Console.WriteLine($"\nImage file path has been set to {parameters.ImageFilePath}.");
            }
            #endregion

            #region ImageIndex
            if (parameters.ImageIndex == -1)
            {
                DeployManager.GetImageInfo(ref parameters);

                Console.Write("\n==> Type the index number of the Windows edition you wish to install (e.g. 1): ");
                parameters.ImageIndex = Convert.ToInt32(Console.ReadLine() ??
                    throw new ArgumentException("No Windows edition was specified."), CultureInfo.CurrentCulture);
            }
            #endregion

            #region FirmwareType
            if (string.IsNullOrWhiteSpace(parameters.FirmwareType))
            {
                parameters.FirmwareType = SystemInfoManager.IsEFI() ? "UEFI" : "BIOS";
                Console.WriteLine($"\nThe installer has set the firmware type to {parameters.FirmwareType}.", ConsoleColor.Yellow);
            }
            #endregion

            #region AdditionalDrive
            if (string.IsNullOrWhiteSpace(parameters.AdditionalDrive))
            {
                Console.Write("\n=> Do you want to add additional drivers to your installation?: [Y/N]: ");
                string? UserWantsExtraDrivers = Console.ReadLine()?.ToLower(CultureInfo.CurrentCulture);

                if (string.IsNullOrEmpty(UserWantsExtraDrivers) ||
                    string.IsNullOrWhiteSpace(UserWantsExtraDrivers))
                {
                    UserWantsExtraDrivers = "no";
                }

                switch (UserWantsExtraDrivers.ToLower())
                {
                    case "yes":
                    case "y":
                        Console.Write("\n==> Specify the directory where the drivers are located. (e.g. X:\\Drivers): ");
                        string? driversPath = Console.ReadLine();
                        if (string.IsNullOrEmpty(driversPath) ||
                            string.IsNullOrWhiteSpace(driversPath))
                        {
                            throw new ArgumentException("No drivers path was specified.");
                        }

                        if (!Directory.Exists(driversPath))
                        {
                            throw new FileNotFoundException($"The directory {driversPath} does not exist");
                        }

                        parameters.AdditionalDrive = driversPath;
                        break;
                    default:
                        return;
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
        public static void Prepare(ref Parameters parameters)
        {
            if (parameters.DiskNumber.Equals(-1))
            {
                throw new InvalidDataException("No disk number was specified, required to know where to install Windows at.");
            }

            if (string.IsNullOrWhiteSpace(parameters.EfiDrive))
            {
                throw new InvalidDataException("No EFI drive was specified, required for the bootloader installation.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.DestinationDrive, parameters.DestinationDrive);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.EfiDrive, parameters.EfiDrive);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ImageFilePath, parameters.ImageFilePath);
            ArgumentOutOfRangeException.ThrowIfEqual(parameters.ImageIndex, -1);
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FirmwareType, parameters.FirmwareType);

            try
            {
                DiskManager.FormatDisk(ref parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while formatting the disk: {ex}", ConsoleColor.Red);
            }
        }

        public static void InstallWindows(ref Parameters parameters)
        {
            try
            {
                DeployManager.ApplyImage(ref parameters);
            }
            catch
            {
                throw;
            }

            try
            {
                DeployManager.InstallAdditionalDrivers(ref parameters);
            }
            catch
            {
                throw;
            }

            try
            {
                DeployManager.InstallBootloader(ref parameters);
            }
            catch
            {
                throw;
            }
        }
    }
}
