﻿using Microsoft.Dism;
using System;
using System.IO;
using System.Runtime.Versioning;
using WindowsInstallerLib.Management.PrivilegesManager;

namespace WindowsInstallerLib.Utilities.Deployment
{
    [SupportedOSPlatform("windows")]
    public partial class NewDeploy
    {
        /// <summary>
        /// Gets all Windows editions available using DISM, if any.
        /// </summary>
        /// <param name="parameters"></param>
        public static void GetImageInfo(string ImageFilePath)
        {
            if (string.IsNullOrEmpty(ImageFilePath))
            {
                throw new FileNotFoundException("No image file was specified.", ImageFilePath);
            }

            switch (GetPrivileges.IsUserAdmin())
            {
                case true:
                    try
                    {
                        DismApi.Initialize(DismLogLevel.LogErrorsWarnings);

                        DismImageInfoCollection images = DismApi.GetImageInfo(ImageFilePath);

                        Console.WriteLine($"\nFound {images.Count} image(s) in {ImageFilePath}", ConsoleColor.Yellow);

                        foreach (DismImageInfo image in images)
                        {
                            Console.WriteLine($"Image index: {image.ImageIndex}\nImage name: {image.ImageName}");
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
                    break;
                case false:
                    throw new UnauthorizedAccessException("Cannot initialize the DISM API without Administrator privileges.");
            }
        }
    }
}
