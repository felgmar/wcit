using System;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;

namespace WindowsInstallerLib
{
    /// <summary>
    /// Provides utility methods for managing and checking user privileges.
    /// </summary>
    /// <remarks>This class includes methods to determine the current user's privilege level, such as whether
    /// the user has administrative rights. It is designed for use on Windows platforms and may throw exceptions if
    /// security or identity-related issues occur.</remarks>
    internal static class PrivilegesManager
    {
        /// <summary>
        /// Determines whether the current user has administrative privileges on a Windows platform.
        /// </summary>
        /// <remarks>This method is only supported on Windows platforms. It uses the <see
        /// cref="WindowsPrincipal"/> and <see cref="WindowsIdentity"/> classes to check if the current user belongs to
        /// the Administrators group.</remarks>
        /// <returns><see langword="true"/> if the current user is a member of the Administrators group; otherwise, <see
        /// langword="false"/>.</returns>
        [SupportedOSPlatform("windows")]
        internal static bool IsAdmin()
        {
            try
            {
                WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return isAdmin;
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (ArgumentException)
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
