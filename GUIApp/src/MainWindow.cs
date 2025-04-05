using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using WindowsInstallerLib;
using WindowsInstallerLib.Helpers;

namespace wit
{
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : Form
    {
        Parameters parameters = new()
        {
            DiskNumber = -1,
            ImageIndex = -1
        };

        [RequiresUnreferencedCode("Calls System.ComponentModel.ComponentResourceManager.ApplyResources(Object, String)")]
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            try
            {
                GetDisksData(sender, e);
                GetDiskLetters(sender, e);

                WindowsEditionIndex.Enabled = false;
                InstallButton.Enabled = false;

                DestinationDrive.SelectedIndexChanged += ValidateDiskLetter;
                EfiDrive.SelectedIndexChanged += ValidateDiskLetter;
                SourceDrive.SelectedIndexChanged += ValidateDiskLetter;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void CloseApplication(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ChooseISOImage_Click(object sender, EventArgs e)
        {
            try
            {
                SourceDrive_Scan.Enabled = false;

                if (ImageFilePath.Text == "No image file is selected.")
                {
                    OpenFileDialog OpenFileDialog = new()
                    {
                        Filter = "ESD file (*.esd)|*.esd|WIM file (*.wim)|*.wim",
                        CheckFileExists = true,
                        CheckPathExists = true,
                        AddExtension = true,
                    };

                    DialogResult DialogResult = OpenFileDialog.ShowDialog();

                    if (DialogResult == DialogResult.Cancel)
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(OpenFileDialog.FileName))
                    {
                        MessageBox.Show("No image file was selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        ImageFilePath.Text = "No image file is selected";

                        return;
                    }

                    if (!OpenFileDialog.FileName.Contains("install"))
                    {
                        MessageBox.Show("Invalid image file. It must be 'install.wim' or 'install.esd'.", "Invalid file", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        throw new InvalidDataException("Invalid image file. It must be 'install.wim' or 'install.esd'.");
                    }

                    ImageFilePath.Text = OpenFileDialog.FileName;

                    parameters.ImageFilePath = ImageFilePath.Text;
                }

                if (ImageList.Columns.Count >= 1)
                {
                    ImageList.Columns.Clear();
                }

                GetImageInfo(sender, e);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SourceDrive_Scan_Click(object sender, EventArgs e)
        {
            try
            {
                if (SourceDrive.Text.Length > 0)
                {
                    foreach (string file in Directory.EnumerateFiles(SourceDrive.Text + @"\sources\"))
                    {
                        if (file.Contains("install") && file.EndsWith(".esd", StringComparison.CurrentCulture) ||
                            file.Contains("install") && file.EndsWith(".wim", StringComparison.CurrentCulture))
                        {
                            ImageFilePath.Text = file;
                            parameters.ImageFilePath = ImageFilePath.Text;
                            ChooseISOImage.Enabled = false;
                            break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a source drive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (ImageList.Columns.Count >= 1)
                {
                    ImageList.Columns.Clear();
                }

                GetImageInfo(sender, e);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            try
            {
                parameters.DestinationDrive = DestinationDrive.Text;
                parameters.EfiDrive = EfiDrive.Text;
                parameters.DiskNumber = Convert.ToInt32(DiskNumber.Value);
                parameters.ImageFilePath = ImageFilePath.Text;
                parameters.ImageIndex = Convert.ToInt32(WindowsEditionIndex.Value);

                if (string.IsNullOrWhiteSpace(parameters.FirmwareType))
                {
                    switch (SystemInfo.SystemSupportsEFI())
                    {
                        case true:
                            parameters.FirmwareType = "UEFI";
                            break;
                        case false:
                            parameters.FirmwareType = "BIOS";
                            break;
                    }
                }
            }
            catch
            {
                throw;
            }

            try
            {
                InstallButton.Text = "Please wait...";
                Deployment.FormatDisk(ref parameters);
                Deployment.ApplyImage(ref parameters);
                Deployment.InstallBootloader(ref parameters);
                InstallButton.Text = "Installation complete";
            }
            catch
            {
                throw;
            }
        }

        private void AboutWindow_Click(object sender, EventArgs e)
        {
            try
            {
                new AboutWindow().ShowDialog(this);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch
            {
                throw;
            }
        }

        private void RescanDisks_Click(object sender, EventArgs e)
        {
            try
            {
                RescanDisks.Text = "Please wait...";
                RescanDisks.Enabled = false;
                DiskList.Columns.Clear();
                DiskList.Rows.Clear();
                GetDisksData(sender, e);
            }
            catch
            {
                throw;
            }
            finally
            {
                RescanDisks.Text = "Rescan disks";
                RescanDisks.Enabled = true;
            }
        }
    }
}
