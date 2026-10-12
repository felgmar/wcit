using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Provides utility methods for managing and executing system processes.
    /// </summary>
    /// <remarks>The <see cref="ProcessManager"/> class includes methods for starting and managing various
    /// types of processes, such as command-line tools, disk partitioning utilities, and deployment tools. It also
    /// tracks the exit code of the last executed process. This class is intended for internal use and is not
    /// thread-safe.</remarks>
    internal sealed class ProcessManager
    {
        /// <summary>
        /// Gets the exit code that represents the result of the process's execution.
        /// </summary>
        internal static int ExitCode { get; private set; }

        /// <summary>
        /// Starts a command-line process with the specified file name and arguments, waits for it to complete, and
        /// returns its exit code.
        /// </summary>
        /// <remarks>The method uses a non-shell execution mode (<see
        /// cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="false"/>). The caller
        /// is responsible for ensuring that the specified file exists and is executable.</remarks>
        /// <param name="fileName">The name or path of the executable file to start. This cannot be null or empty.</param>
        /// <param name="args">The command-line arguments to pass to the process. This can be null or an empty string if no arguments are
        /// required.</param>
        /// <returns>The exit code of the process after it has completed execution.</returns>
        internal static int StartCmdProcess(string fileName, params string[] args)
        {
            using Process process = new();
            process.StartInfo.FileName = fileName;
            process.StartInfo.UseShellExecute = false;
            foreach (string argument in args)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                throw new InvalidOperationException($"Could not start {fileName}.");
            }

            process.WaitForExit();
            ExitCode = process.ExitCode;
            return ExitCode;
        }

        /// <summary>
        /// Executes a DiskPart process to format and partition a specified disk.
        /// </summary>
        /// <remarks>This method uses the DiskPart utility to perform the following operations on the
        /// specified disk: <list type="bullet"> <item><description>Selects the specified disk.</description></item>
        /// <item><description>Cleans the disk.</description></item> <item><description>Converts the disk to GPT
        /// format.</description></item> <item><description>Creates an MSR partition.</description></item>
        /// <item><description>Creates and formats an EFI partition with the FAT32 file system.</description></item>
        /// <item><description>Assigns the specified drive letter to the EFI partition.</description></item>
        /// <item><description>Creates and formats a primary partition with the NTFS file system.</description></item>
        /// <item><description>Assigns the specified drive letter to the primary partition.</description></item> </list>
        /// The method writes commands to the DiskPart process via standard input and waits for the process to
        /// complete.</remarks>
        /// <param name="DiskNumber">The number of the disk to be formatted and partitioned.</param>
        /// <param name="EfiDrive">The drive letter to assign to the EFI partition.</param>
        /// <param name="DestinationDrive">The drive letter to assign to the primary partition.</param>
        /// <returns>The exit code of the DiskPart process. A value of 0 indicates success, while a non-zero value indicates
        /// failure.</returns>
        internal static int StartDiskPartProcess(
            int DiskNumber,
            string EfiDrive,
            string DestinationDrive,
            string FirmwareType)
        {
            if (!string.Equals(FirmwareType, "UEFI", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(FirmwareType, "BIOS", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Unsupported firmware type: {FirmwareType}");
            }

            using Process process = new();
            process.StartInfo.FileName = "diskpart.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            if (!process.Start())
            {
                throw new InvalidOperationException("DiskPart could not be started.");
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            process.StandardInput.WriteLine($"select disk {DiskNumber}");
            process.StandardInput.WriteLine("clean");
            if (string.Equals(FirmwareType, "UEFI", StringComparison.OrdinalIgnoreCase))
            {
                process.StandardInput.WriteLine("convert gpt");
                process.StandardInput.WriteLine("create partition efi size=100");
                process.StandardInput.WriteLine("format fs=fat32 quick");
                process.StandardInput.WriteLine($"assign letter {EfiDrive.TrimEnd(':')}");
                process.StandardInput.WriteLine("create partition msr size=16");
            }
            else if (string.Equals(FirmwareType, "BIOS", StringComparison.OrdinalIgnoreCase))
            {
                process.StandardInput.WriteLine("convert mbr");
                process.StandardInput.WriteLine("create partition primary size=500");
                process.StandardInput.WriteLine("format fs=ntfs quick");
                process.StandardInput.WriteLine("active");
                process.StandardInput.WriteLine($"assign letter {EfiDrive.TrimEnd(':')}");
            }
            process.StandardInput.WriteLine("create partition primary");
            process.StandardInput.WriteLine("format fs=ntfs quick");
            process.StandardInput.WriteLine($"assign letter {DestinationDrive.TrimEnd(':')}");
            process.StandardInput.WriteLine("exit");
            process.StandardInput.Close();
            process.WaitForExit();
            string output = outputTask.GetAwaiter().GetResult();
            ExitCode = process.ExitCode;

            if (output.Contains("DiskPart has encountered an error", StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException($"DiskPart reported an error while partitioning disk {DiskNumber}:{Environment.NewLine}{output}");
            }

            if (ExitCode != 0)
            {
                throw new IOException($"DiskPart failed with exit code {ExitCode} while partitioning disk {DiskNumber}.");
            }

            Console.WriteLine($"Disk {DiskNumber} has been formatted successfully.");
            return ExitCode;
        }

        /// <summary>
        /// Starts a new process to execute the Deployment Image Servicing and Management (DISM) tool with the specified
        /// arguments.
        /// </summary>
        /// <remarks>This method starts the DISM tool as a separate process and waits for it to complete
        /// execution.  The caller is responsible for ensuring that the provided arguments are valid and appropriate for
        /// the DISM tool.</remarks>
        /// <param name="args">The command-line arguments to pass to the DISM tool. This must be a valid string of arguments supported by
        /// DISM.</param>
        /// <returns>The exit code returned by the DISM process. A value of 0 typically indicates success, while non-zero values
        /// indicate an error or failure.</returns>
        internal static int StartDismProcess(params string[] args)
        {
            using Process process = new();
            process.StartInfo.FileName = "dism.exe";
            process.StartInfo.UseShellExecute = false;
            foreach (string argument in args)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                throw new InvalidOperationException("DISM could not be started.");
            }

            process.WaitForExit();
            ExitCode = process.ExitCode;
            return ExitCode;
        }

        /// <summary>
        /// Starts a process with the specified executable file and arguments, waits for it to exit,  and returns the
        /// process's exit code.
        /// </summary>
        /// <remarks>The method uses a non-shell execution model (<see
        /// cref="System.Diagnostics.ProcessStartInfo.UseShellExecute"/> is set to <see langword="false"/>). The caller
        /// is responsible for ensuring that the specified executable file exists and is accessible.</remarks>
        /// <param name="filename">The path to the executable file to start. This cannot be null or empty.</param>
        /// <param name="args">The command-line arguments to pass to the executable. This can be null or empty if no arguments are
        /// required.</param>
        /// <returns>The exit code of the process after it has completed execution.</returns>
        internal static int StartProcess(string filename, string args)
        {
            Process process = new();

            try
            {
                process.StartInfo.FileName = filename;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
                ExitCode = process.ExitCode;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Win32Exception)
            {
                throw;
            }
            catch (PlatformNotSupportedException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                process.Close();
            }

            return ExitCode;
        }
    }
}
