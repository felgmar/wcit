using Microsoft.Dism;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Provides methods for managing the deployment of Windows images, including adding drivers, applying images,
    /// retrieving image information, and installing the bootloader.
    /// </summary>
    /// <remarks>This class is designed to work with Windows Deployment Image Servicing and Management (DISM)
    /// APIs and requires administrative privileges for most operations. It is supported only on Windows
    /// platforms.</remarks>
    [SupportedOSPlatform("windows")]
    internal static class DeployManager
    {
        /// <summary>
        /// Adds one or more drivers to the specified offline Windows image.
        /// </summary>
        /// <remarks>This method initializes the DISM API, opens an offline session for the specified
        /// destination drive, and adds the provided drivers. If <paramref name="DriversSource"/> is an array, all
        /// drivers in the array are added recursively. Otherwise, a single driver is added. The DISM API is properly
        /// shut down after the operation completes, even if an exception occurs.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object containing details about the image file path and
        /// destination drive. The <see cref="Parameters.ImageFilePath"/> and <see cref="Parameters.DestinationDrive"/>
        /// properties must not be null, empty, or whitespace.</param>
        /// <param name="DriversSource">The source path of the driver or drivers to be added. This can be a single driver file path or an array of
        /// driver paths. The value must not be null, empty, or whitespace.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if the directory specified in <see cref="Parameters.DestinationDrive"/> does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current process does not have administrative privileges required to initialize the DISM API.</exception>
        internal static void AddDrivers(ref Parameters parameters,
                                        string DriversSource,
                                        bool ForceUnsigned = false,
                                        bool Recursive = false)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ImageFilePath, nameof(parameters.ImageFilePath));
            ArgumentException.ThrowIfNullOrWhiteSpace(DriversSource, nameof(DriversSource));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.DestinationDrive, nameof(parameters.DestinationDrive));

            if (!PrivilegesManager.IsAdmin())
            {
                throw new UnauthorizedAccessException("You do not have enough privileges to initialize the DISM API.");
            }

            if (!Directory.Exists(parameters.DestinationDrive))
            {
                throw new DirectoryNotFoundException($"Could not find the directory: {parameters.DestinationDrive}");
            }

            try
            {
                DismApi.Initialize(DismLogLevel.LogErrorsWarningsInfo);
                DismSession session = DismApi.OpenOfflineSession(parameters.DestinationDrive);

                if (DriversSource.GetType().IsArray)
                {
                    DismApi.AddDriversEx(session, DriversSource, ForceUnsigned, Recursive);
                }
                else
                {
                    DismApi.AddDriver(session, DriversSource, ForceUnsigned);
                }
            }
            finally
            {
                DismApi.Shutdown();
            }
        }

        /// <summary>
        /// Applies a Windows image to the specified destination drive.
        /// </summary>
        /// <remarks>This method uses the Deployment Image Servicing and Management (DISM) tool to apply
        /// the specified image. Ensure that the destination drive does not already contain a Windows deployment, as the
        /// method will not overwrite an existing installation. Administrative privileges are required to execute this
        /// operation.</remarks>
        /// <param name="parameters">A <see cref="Parameters"/> object containing the necessary details for the operation, including the
        /// destination drive, image file path, disk number, and image index.</param>
        /// <returns>An integer representing the exit code of the operation. Returns <c>1</c> if the destination drive already
        /// contains a Windows deployment. Otherwise, returns the exit code of the underlying process.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have administrative privileges required to perform the operation.</exception>
        internal static int ApplyImage(ref Parameters parameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.DestinationDrive, nameof(parameters.DestinationDrive));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.ImageFilePath, nameof(parameters.ImageFilePath));
            ArgumentOutOfRangeException.ThrowIfEqual(parameters.DiskNumber, -1, nameof(parameters.DiskNumber));
            ArgumentOutOfRangeException.ThrowIfEqual(parameters.ImageIndex, -1, nameof(parameters.ImageIndex));

            if (Directory.Exists($@"{parameters.DestinationDrive}\windows"))
            {
                Console.Error.WriteLine("Windows seems to be already deployed, not overwriting it.");
                return 1;
            }

            if (!PrivilegesManager.IsAdmin())
            {
                throw new UnauthorizedAccessException($"You do not have enough privileges to deploy Windows to {parameters.DestinationDrive}.");
            }

            Console.WriteLine($"\n==> Deploying Windows to drive {parameters.DestinationDrive} in disk {parameters.DiskNumber}, please wait...");
            ProcessManager.StartDismProcess(@$"/apply-image /imagefile:{parameters.ImageFilePath} /applydir:{parameters.DestinationDrive} /index:{parameters.ImageIndex} /verify");
            return ProcessManager.ExitCode;
        }

        /// <summary>
        /// Determines the path to a valid image file (either "install.esd" or "install.wim")  located in the "sources"
        /// directory of the specified source drive.
        /// </summary>
        /// <remarks>This method checks for the presence of "install.esd" and "install.wim" files in the
        /// "sources" directory of the drive specified by <see cref="Parameters.SourceDrive"/>. If both files are
        /// present, "install.esd" is returned.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object containing the source drive and image file path. The <see
        /// cref="Parameters.SourceDrive"/> property must specify the root directory of the source drive.</param>
        /// <returns>The full path to the image file ("install.esd" or "install.wim") if found.</returns>
        internal static string GetImageFile(ref Parameters parameters)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(nameof(parameters.SourceDrive));
                ArgumentException.ThrowIfNullOrWhiteSpace(nameof(parameters.ImageFilePath));

                string IMAGE_FILE_ESD = Path.Join(parameters.SourceDrive, @"\sources\install.esd");
                string IMAGE_FILE_WIM = Path.Join(parameters.SourceDrive, @"\sources\install.wim");

                bool IS_IMAGE_FILE_ESD = File.Exists(IMAGE_FILE_ESD);
                bool IS_IMAGE_FILE_WIM = File.Exists(IMAGE_FILE_WIM);

                if (IS_IMAGE_FILE_ESD)
                {
                    return IMAGE_FILE_ESD;
                }
                else if (IS_IMAGE_FILE_WIM)
                {
                    return IMAGE_FILE_WIM;
                }
                else
                {
                    throw new FileNotFoundException($"Could not find a valid image file at {parameters.SourceDrive}.");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Retrieves and displays information about the images contained in the specified image file.
        /// </summary>
        /// <remarks>This method initializes the DISM API, retrieves image information from the specified
        /// file,  and outputs details about each image to the console. The method requires administrative privileges 
        /// to execute and will throw an exception if the caller lacks sufficient privileges.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object containing the path to the image file. The <see
        /// cref="Parameters.ImageFilePath"/> property must not be null or empty.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the caller does not have administrative privileges.</exception>
        internal static void GetImageInfo(ref Parameters parameters)
        {
            ArgumentException.ThrowIfNullOrEmpty(parameters.ImageFilePath, nameof(parameters));

            if (!PrivilegesManager.IsAdmin())
            {
                throw new UnauthorizedAccessException("You do not have enough privileges to initialize the DISM API.");
            }

            try
            {
                DismApi.Initialize(DismLogLevel.LogErrorsWarnings);

                DismImageInfoCollection images = DismApi.GetImageInfo(parameters.ImageFilePath);

                switch (images.Count)
                {
                    case > 1:
                        Console.WriteLine($"\nFound {images.Count} images in {parameters.ImageFilePath}, shown below.\n", ConsoleColor.Yellow);
                        break;
                    case 1:
                        Console.WriteLine($"\nFound {images.Count} image in {parameters.ImageFilePath}, shown below.\n", ConsoleColor.Yellow);
                        break;
                    case 0:
                        Console.WriteLine($"\nNo images were found in {parameters.ImageFilePath}\n", ConsoleColor.Red);
                        throw new InvalidDataException($"images.Count is {images.Count}. This is considered to be invalid, the program cannot continue.");
                }

                foreach (DismImageInfo image in images)
                {
                    Console.WriteLine($"Index: {image.ImageIndex}");
                    Console.WriteLine($"Name: {image.ImageName}");
                    Console.WriteLine($"Size: {image.ImageSize}");
                    Console.WriteLine($"Arch: {image.Architecture}\n");
                }
            }
            catch (DismException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                DismApi.Shutdown();
            }
        }

        /// <summary>
        /// Retrieves information about the images contained in the specified image file.
        /// </summary>
        /// <remarks>This method initializes the DISM API to retrieve image information and ensures proper
        /// shutdown of the API after the operation is complete. Ensure that the caller has sufficient privileges to
        /// execute this method.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object that contains the path to the image file. The <see
        /// cref="Parameters.ImageFilePath"/> property must not be null, empty, or consist only of whitespace.</param>
        /// <returns>A <see cref="DismImageInfoCollection"/> containing details about the images in the specified file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the <see cref="Parameters.ImageFilePath"/> is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current user does not have administrative privileges required to initialize the DISM API.</exception>
        internal static DismImageInfoCollection GetImageInfoT(ref Parameters parameters)
        {
            if (string.IsNullOrEmpty(parameters.ImageFilePath) ||
                string.IsNullOrWhiteSpace(parameters.ImageFilePath))
            {
                throw new FileNotFoundException("No image file was specified.", parameters.ImageFilePath);
            }

            switch (PrivilegesManager.IsAdmin())
            {
                case true:
                    DismApi.Initialize(DismLogLevel.LogErrorsWarnings);
                    break;
                case false:
                    throw new UnauthorizedAccessException("You do not have enough privileges to initialize the DISM API.");
            }

            try
            {
                DismImageInfoCollection images = DismApi.GetImageInfo(parameters.ImageFilePath);

                return images;
            }
            catch (DismException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                DismApi.Shutdown();
            }
        }

        /// <summary>
        /// Installs additional drivers to the specified offline Windows image.
        /// </summary>
        /// <param name="parameters"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        internal static void InstallAdditionalDrivers(ref Parameters parameters)
        {
            DismSession session;

            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.AdditionalDriversDrive, nameof(parameters));

            if (!PrivilegesManager.IsAdmin())
            {
                throw new UnauthorizedAccessException("You do not have enough privileges to install additional drivers.");
            }

            try
            {
                switch (PrivilegesManager.IsAdmin())
                {
                    case true:
                        DismApi.Initialize(DismLogLevel.LogErrorsWarnings);
                        break;
                    case false:
                        throw new UnauthorizedAccessException("You do not have enough privileges to initialize the DISM API.");
                }

                session = DismApi.OpenOfflineSession(parameters.DestinationDrive);

            }
            catch (DismException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                DismApi.AddDriversEx(session, parameters.AdditionalDriversDrive, false, true);
            }
            finally
            {
                session.Close();
                DismApi.Shutdown();
            }
        }

        /// <summary>
        /// Installs the bootloader to the specified EFI system partition.
        /// </summary>
        /// <remarks>This method requires administrative privileges to execute. Ensure that the calling
        /// process has sufficient permissions. The method validates the existence of required directories and checks
        /// for conflicts before proceeding with the installation.</remarks>
        /// <param name="parameters">A reference to a <see cref="Parameters"/> object containing the configuration for the bootloader
        /// installation. The <see cref="Parameters.DestinationDrive"/> property specifies the drive containing the
        /// Windows installation. The <see cref="Parameters.EfiDrive"/> property specifies the EFI system partition
        /// where the bootloader will be installed. The <see cref="Parameters.FirmwareType"/> property specifies the
        /// firmware type (e.g., "UEFI" or "BIOS").</param>
        /// <returns>The exit code of the bootloader installation process. A value of 0 typically indicates success.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown if the current process does not have administrative privileges required to perform the installation.</exception>
        internal static int InstallBootloader(ref Parameters parameters)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.DestinationDrive, nameof(parameters.DestinationDrive));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.EfiDrive, nameof(parameters.EfiDrive));
            ArgumentException.ThrowIfNullOrWhiteSpace(parameters.FirmwareType, nameof(parameters.FirmwareType));

            string EFI_BOOT_PATH = Path.Join(parameters.EfiDrive, @"\EFI\Boot");
            string EFI_MICROSOFT_PATH = Path.Join(parameters.EfiDrive, @"\EFI\Microsoft");
            string WINDIR_PATH = Path.Join(parameters.DestinationDrive, @"\windows");

            bool EFI_BOOT_EXISTS = Directory.Exists(EFI_BOOT_PATH);
            bool EFI_MICROSOFT_EXISTS = Directory.Exists(EFI_MICROSOFT_PATH);
            bool WINDIR_EXISTS = Directory.Exists(WINDIR_PATH);

            if (!PrivilegesManager.IsAdmin())
            {
                throw new UnauthorizedAccessException($"You do not have enough privileges to install the bootloader to {parameters.EfiDrive}");
            }

            try
            {
                if (EFI_BOOT_EXISTS || EFI_MICROSOFT_EXISTS)
                {
                    throw new IOException($"The drive letter {parameters.EfiDrive} is already in use.");
                }

                if (!WINDIR_EXISTS)
                {
                    throw new DirectoryNotFoundException(@$"The directory {WINDIR_PATH} does not exist!");
                }

                Console.WriteLine($"Firmware type is set to: {parameters.FirmwareType}");
                Console.WriteLine($"\n==> Installing bootloader to drive {parameters.EfiDrive} in disk {parameters.DiskNumber}");
                ProcessManager.StartCmdProcess("bcdboot", @$"{WINDIR_PATH} /s {parameters.EfiDrive} /f {parameters.FirmwareType}");
            }
            catch (IOException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return ProcessManager.ExitCode;
        }
    }
}
