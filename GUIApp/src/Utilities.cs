using Microsoft.Dism;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using WindowsInstallerLib.Helpers;

namespace wit
{
    public partial class MainWindow
    {
        private void GetDiskLetters(object sender, EventArgs e)
        {
            List<string> DiskLetters = [
                "A:\\",
                "B:\\",
                "C:\\",
                "D:\\",
                "E:\\",
                "F:\\",
                "G:\\",
                "H:\\",
                "I:\\",
                "J:\\",
                "K:\\",
                "L:\\",
                "M:\\",
                "N:\\",
                "O:\\",
                "P:\\",
                "Q:\\",
                "R:\\",
                "S:\\",
                "T:\\",
                "U:\\",
                "V:\\",
                "W:\\",
                "X:\\",
                "Y:\\",
                "Z:\\"
            ];
            List<string> DiskLettersToRemove = [];
            List<Tuple<int, string, string>> Disks = Devices.GetDisks();

            try
            {
                foreach (string DiskLetter in DiskLetters)
                {
                    foreach (Tuple<int, string, string> disk in Disks)
                    {
                        string Letter = disk.Item3 + @"\";

                        if (DiskLetter.Equals(Letter, StringComparison.Ordinal))
                        {
                            DiskLettersToRemove.Add(Letter);
                        }
                    }
                }

                foreach (string DiskLetter in DiskLettersToRemove)
                {
                    DiskLetters.Remove(DiskLetter);
                }

                DestinationDrive.Items.AddRange(DiskLetters.ToArray());
                EfiDrive.Items.AddRange(DiskLetters.ToArray());
                SourceDrive.Items.AddRange(DiskLetters.ToArray());
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void GetDisksData(object sender, EventArgs e)
        {
            DiskList.Columns.Add("DiskNumber", "Disk");
            DiskList.Columns.Add("Model", "Model");
            DiskList.Columns.Add("Label", "Label");

            List<Tuple<int, string, string>> disks = Devices.GetDisks();

            foreach (Tuple<int, string, string> disk in disks)
            {
                try
                {
                    DiskList.Rows.Add(disk.Item1, disk.Item2, disk.Item3);
                }
                catch (ArgumentNullException)
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

            DiskList.Sort(DiskList.Columns[0], ListSortDirection.Ascending);
            DiskList.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            DiskNumber.Maximum = DiskList.Rows.Count - 1;
        }

        private void GetImageInfo(object sender, EventArgs e)
        {
            try
            {
                DismImageInfoCollection imageInfoCollection = Deployment.GetImageInfo(ref parameters);

                WindowsEditionIndex.Minimum = 1;
                WindowsEditionIndex.Maximum = imageInfoCollection.Count;

                WindowsEditionIndex.Enabled = true;

                ImageList.Columns.Add("Index", "Index");
                ImageList.Columns.Add("Edition", "Name");
                ImageList.Columns.Add("Version", "Version");

                foreach (DismImageInfo DismImage in imageInfoCollection)
                {
                    ImageList.Rows.Add(DismImage.ImageIndex, DismImage.ImageName, DismImage.ProductVersion);
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
    }
}
