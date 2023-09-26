﻿using Runtime.Management.DiskManagement;
using Runtime.Utilities.Deployment;
using System;

namespace Runtime.Management.ParametersManager
{
    sealed class Parameters
    {
        public static string? DestinationDrive { get; set; }
        public static string? EfiDrive { get; set; }
        public static int DiskNumber = -1;
        public static string? SourceDrive { get; set; }
        public static int WindowsEdition = 0;
        public static bool AddDriversToWindows = false;

        internal static void Setup()
        {
            if (DestinationDrive == null)
            {
                Console.Write("\n==> Type the mountpoint to use for deploying Windows (e.g. Z:): ");
                DestinationDrive = Console.ReadLine();

                if (string.IsNullOrEmpty(DestinationDrive))
                {
                    Console.Write("No destination drive was specified.\n\nPress ENTER to quit the program: ");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
                else if (!DestinationDrive.Contains(':'))
                {
                    throw new ArgumentException($"Invalid source drive {SourceDrive}, it must have a colon. For example: 'H:'.");
                }
            }

            if (EfiDrive == null)
            {
                Console.Write("\n==> Type a mountpoint for installing the bootloader at (e.g. K:): ");
                EfiDrive = Console.ReadLine();

                if (string.IsNullOrEmpty(EfiDrive))
                {
                    throw new ArgumentException("No EFI drive was specified");
                }
                else if (!EfiDrive.Contains(':'))
                {
                    throw new ArgumentException($"Invalid EFI drive {EfiDrive}, it must have a colon. For example: 'H:'.");
                }
            }

            if (DiskNumber == -1)
            {
                Console.WriteLine("\n==> These are the disks available on your system:");
                SystemDrives.ListAll();

                Console.Write("\n==> Please type the disk number to format (e.g. 0): ");
                string? SelectedDisk = Console.ReadLine();

                if (!string.IsNullOrEmpty(SelectedDisk))
                {
                    DiskNumber = Convert.ToInt32(SelectedDisk);
                }
                else
                {
                    throw new ArgumentException("No disk was chosen to formatting.", nameof(DiskNumber));
                }


                // This prevents SourceDrive = Console.ReadLine() shown below from being skipped
                Console.ReadLine();
            }

            if (SourceDrive == null)
            {
                Console.Write("\n==> Type the letter where the ISO is mounted at (e.g. D:): ");
                SourceDrive = Console.ReadLine();

                if (string.IsNullOrEmpty(SourceDrive))
                {
                    throw new ArgumentException("No source drive was specified.\n\nPress ENTER to quit the program.", nameof(SourceDrive));
                }
                else if (!SourceDrive.Contains(':'))
                {
                   throw new ArgumentException($"Invalid source drive {SourceDrive}, it must have a colon. For example: 'H:'.");
                }
            }

            if (WindowsEdition == 0)
            {
                NewDeploy.GetImageInfo(SourceDrive);

                Console.Write("==> Type the index number of the Windows edition you wish to install (e.g. 1): ");
                string? SelectedIndex = Console.ReadLine();

                if (!string.IsNullOrEmpty(SelectedIndex))
                {
                    WindowsEdition = Convert.ToInt32(SelectedIndex);
                }
                else
                {
                    throw new ArgumentException("No Windows edition was specified.", nameof(SelectedIndex));
                }
            }
        }
    }
}
