﻿using System;
using System.ComponentModel;
using System.Diagnostics;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Handles the creation and management of processes.
    /// </summary>
    internal sealed class ProcessManager
    {
        internal static int ExitCode { get; private set; }

        internal static int StartCmdProcess(string fileName, string args)
        {
            try
            {
                Process process = new();
                process.StartInfo.FileName = fileName;
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
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            return ExitCode;
        }

        internal static int StartDiskPartProcess(int DiskNumber, string EfiDrive, string DestinationDrive)
        {
            Process process = new();

            try
            {
                process.StartInfo.FileName = "diskpart.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                Console.WriteLine($"Formatting disk {DiskNumber}, please wait...");

                process.StandardInput.WriteLine($"select disk {DiskNumber}");
                process.StandardInput.WriteLine("clean");
                process.StandardInput.WriteLine("convert gpt");
                process.StandardInput.WriteLine("create partition msr size=16");
                process.StandardInput.WriteLine("create partition efi size=100");
                process.StandardInput.WriteLine("format fs=fat32 quick");
                process.StandardInput.WriteLine($"assign letter {EfiDrive}");
                process.StandardInput.WriteLine("create partition primary");
                process.StandardInput.WriteLine("format fs=ntfs quick");
                process.StandardInput.WriteLine($"assign letter {DestinationDrive}");
                process.StandardInput.WriteLine("exit");

                process.WaitForExit();
                ExitCode = process.ExitCode;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Win32Exception)
            {
                throw;
            }
            catch (SystemException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            finally
            {
                switch (ExitCode)
                {
                    case 0:
                        Console.WriteLine($"\nDisk {DiskNumber} has been formatted successfully.");
                        break;
                    case 1:
                        Console.Error.WriteLine($"\nFailed to format the disk {DiskNumber}.");
                        break;
                }
                process.Close();

            }

            return ExitCode;
        }

        internal static int StartDismProcess(string args)
        {
            Process process = new();

            try
            {
                process.StartInfo.FileName = "dism.exe";
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
            finally
            {
                process.Close();
            }

            return ExitCode;
        }

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
