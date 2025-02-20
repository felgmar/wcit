using System.Windows.Forms;
using System;

namespace wit
{
    public partial class MainWindow
    {
        private static void ShowMessage(string text)
        {
            MessageBox.Show(text, "Duplicate letters", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ValidateDiskLetter(object? sender, EventArgs e)
        {
            if (DestinationDrive.Text.Length > 0)
            {
                if (DestinationDrive.Text == EfiDrive.Text)
                {
                    ShowMessage("The OS drive letter cannot be the same as the bootloader drive.");
                    DestinationDrive.Text = null;
                    EfiDrive.Text = null;
                }
                else if (DestinationDrive.Text == SourceDrive.Text)
                {
                    ShowMessage("The OS drive letter cannot be the same as the source drive.");
                    DestinationDrive.Text = null;
                    SourceDrive.Text = null;
                }
            }

            if (EfiDrive.Text.Length > 0)
            {
                if (EfiDrive.Text == DestinationDrive.Text)
                {
                    ShowMessage("The bootloader drive letter cannot be the same as the OS drive.");
                    EfiDrive.Text = null;
                    DestinationDrive.Text = null;
                }
                else if (EfiDrive.Text == SourceDrive.Text)
                {
                    ShowMessage("The bootloader drive letter cannot be the same as the source drive.");
                    EfiDrive.Text = null;
                    SourceDrive.Text = null;
                }
            }

            if (SourceDrive.Text.Length > 0)
            {
                if (SourceDrive.Text == DestinationDrive.Text)
                {
                    ShowMessage("The source drive letter cannot be the same as the OS drive.");
                    SourceDrive.Text = null;
                    DestinationDrive.Text = null;
                }
                else if (SourceDrive.Text == EfiDrive.Text)
                {
                    ShowMessage("The source drive letter cannot be the same as the bootloader drive.");
                    SourceDrive.Text = null;
                    EfiDrive.Text = null;
                }
            }
        }
    }
}
