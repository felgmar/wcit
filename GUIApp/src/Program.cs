using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace wit
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        [RequiresUnreferencedCode("Calls wit.MainWindow.MainWindow()")]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainWindow());
        }
    }
}
