using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Storage.Pickers;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WindowsInstallerLib;
using WinRT.Interop;
using Microsoft.Win32;

namespace WindowsInstallerUI;

public sealed partial class MainWindow : Window
{
    private List<string> _availableDriveLetters = [];
    private int _editionScanVersion;
    private bool _isLoadingDestinationOptions;
    private bool _isUpdatingDriveChoices;
    private int? _targetDiskNumber;
    private readonly UISettings _uiSettings = new();
    private ElementTheme _selectedTheme = ElementTheme.Default;

    public MainWindow()
    {
        InitializeComponent();
        MediaTypeBox.SelectedIndex = 0;

        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        AppWindow appWindow = AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(windowHandle));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragArea);
        appWindow.Resize(new SizeInt32(1024, 768));
        _uiSettings.ColorValuesChanged += SystemColors_Changed;
        ApplySelectedTheme();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySelectedTheme();

        if (_isLoadingDestinationOptions)
        {
            return;
        }

        _isLoadingDestinationOptions = true;
        DestinationStatus.Text = "Scanning physical disks and available drive letters...";

        try
        {
            (SortedDictionary<int, string> disks, List<string> driveLetters) = await Task.Run(
                () => (InstallerManager.GetDisks(), InstallerManager.GetAvailableDriveLetters()));

            TargetDiskBox.ItemsSource = disks
                .Select(disk => new DiskOption(disk.Key, disk.Value))
                .ToArray();
            TargetDiskBox.IsEnabled = disks.Count > 0;
            if (disks.Count > 0)
            {
                TargetDiskBox.SelectedIndex = 0;
            }

            _availableDriveLetters = driveLetters;
            WindowsDriveBox.ItemsSource = _availableDriveLetters;
            WindowsDriveBox.IsEnabled = _availableDriveLetters.Count > 0;
            EfiDriveBox.ItemsSource = _availableDriveLetters;
            EfiDriveBox.IsEnabled = _availableDriveLetters.Count > 1;

            if (_availableDriveLetters.Count > 0)
            {
                WindowsDriveBox.SelectedIndex = 0;
                EfiDriveBox.SelectedIndex = _availableDriveLetters.Count > 1 ? 1 : 0;
            }

            DestinationStatus.Text = disks.Count == 0
                ? "No physical disks were found."
                : _availableDriveLetters.Count < 2
                    ? "At least two unused drive letters are needed."
                    : $"Found {disks.Count} physical disk(s) and {_availableDriveLetters.Count} unused drive letter(s).";
        }
        catch (Exception ex)
        {
            DestinationStatus.Text = $"Could not load disks or drive letters: {ex.Message}";
        }
        finally
        {
            _isLoadingDestinationOptions = false;
        }
    }

    private void SystemThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Default);
    }

    private void LightThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Light);
    }

    private void DarkThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Dark);
    }

    private void SetSelectedTheme(ElementTheme theme)
    {
        _selectedTheme = theme;
        SystemThemeItem.IsChecked = theme == ElementTheme.Default;
        LightThemeItem.IsChecked = theme == ElementTheme.Light;
        DarkThemeItem.IsChecked = theme == ElementTheme.Dark;
        RootLayout.DispatcherQueue.TryEnqueue(ApplySelectedTheme);
    }

    private void SystemColors_Changed(UISettings sender, object args)
    {
        if (_selectedTheme == ElementTheme.Default)
        {
            RootLayout.DispatcherQueue.TryEnqueue(ApplySelectedTheme);
        }
    }

    private void ApplySelectedTheme()
    {
        if (_selectedTheme != ElementTheme.Default)
        {
            RootLayout.RequestedTheme = _selectedTheme;
            return;
        }

        using RegistryKey? personalizationKey = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        object? appsUseLightTheme = personalizationKey?.GetValue("AppsUseLightTheme");
        RootLayout.RequestedTheme = appsUseLightTheme is not null &&
                                   Convert.ToInt32(appsUseLightTheme) != 0
            ? ElementTheme.Light
            : ElementTheme.Dark;
    }

    private async void BrowseIso_Click(object sender, RoutedEventArgs e)
    {
        FileOpenPicker picker = new();
        picker.FileTypeFilter.Add(".iso");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            MediaTypeBox.SelectedIndex = 0;
            IsoPathBox.Text = file.Path;
            ResetImageSearch();
        }
    }

    private void MediaType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool useIso = MediaTypeBox.SelectedIndex == 0;
        IsoSourcePanel.Visibility = useIso ? Visibility.Visible : Visibility.Collapsed;
        DriveSourcePanel.Visibility = useIso ? Visibility.Collapsed : Visibility.Visible;

        if (useIso)
        {
            DriveLetterBox.Text = string.Empty;
        }
        else
        {
            IsoPathBox.Text = string.Empty;
        }

        ResetImageSearch();
    }

    private void DriveLetter_TextChanged(object sender, TextChangedEventArgs e)
    {
        ResetImageSearch();
    }

    private async void FindImage_Click(object sender, RoutedEventArgs e)
    {
        FindImageButton.IsEnabled = false;
        ImageFilesBox.IsEnabled = false;
        ImageSearchStatus.Text = "Preparing installation media...";
        StatusBar.IsOpen = false;
        ReviewCard.Visibility = Visibility.Collapsed;

        try
        {
            string mediaRoot;
            if (MediaTypeBox.SelectedIndex == 0)
            {
                string isoPath = IsoPathBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(isoPath))
                {
                    ShowValidationError("Choose a Windows ISO file.");
                    return;
                }

                if (!File.Exists(isoPath) ||
                    !string.Equals(Path.GetExtension(isoPath), ".iso", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FileNotFoundException("Select an existing .iso file.", isoPath);
                }

                mediaRoot = await Task.Run(() => MountIso(isoPath));
                ImageSearchStatus.Text = $"ISO mounted at {mediaRoot} Searching for Windows images...";
            }
            else
            {
                string driveInput = DriveLetterBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(driveInput))
                {
                    ShowValidationError("Enter the drive letter of your mounted installation media.");
                    return;
                }

                mediaRoot = NormalizeDriveRoot(driveInput);
                if (!Directory.Exists(mediaRoot))
                {
                    throw new DirectoryNotFoundException($"The drive {mediaRoot} is not available.");
                }

                ImageSearchStatus.Text = $"Searching {mediaRoot} for Windows images...";
            }

            string[] imageFiles = await Task.Run(() => FindWindowsImages(mediaRoot));
            if (imageFiles.Length == 0)
            {
                ImageFilesBox.ItemsSource = null;
                ImageFilesBox.IsEnabled = false;
                ImageSearchStatus.Text = $"No install.wim or install.esd files were found under {mediaRoot}.";
                return;
            }

            ImageFilesBox.ItemsSource = imageFiles;
            ImageFilesBox.SelectedIndex = 0;
            ImageFilesBox.IsEnabled = true;
            ImageSearchStatus.Text = $"Found {imageFiles.Length} Windows image file(s) under {mediaRoot}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or ArgumentException)
        {
            ShowValidationError(ex.Message);
        }
        finally
        {
            FindImageButton.IsEnabled = true;
        }
    }

    private async void ImageFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ReviewCard.Visibility = Visibility.Collapsed;
        StatusBar.IsOpen = false;

        if (ImageFilesBox.SelectedItem is string imagePath)
        {
            await ScanImageEditionsAsync(imagePath);
        }
    }

    private void ReviewSetup_Click(object sender, RoutedEventArgs e)
    {
        if (ImageFilesBox.SelectedItem is not string imagePath)
        {
            ShowValidationError("Find and select a Windows image file before reviewing the setup.");
            return;
        }

        if (ImageEditionBox.SelectedItem is not KeyValuePair<int, string> edition)
        {
            ShowValidationError("Scan the selected image and choose a Windows edition.");
            return;
        }

        if (_targetDiskNumber is not int targetDiskNumber)
        {
            ShowValidationError("Select a target installation disk.");
            return;
        }

        if (WindowsDriveBox.SelectedItem is not string windowsDrive ||
            EfiDriveBox.SelectedItem is not string efiDrive ||
            string.Equals(windowsDrive, efiDrive, StringComparison.OrdinalIgnoreCase))
        {
            ShowValidationError("Select different unused drive letters for Windows and EFI.");
            return;
        }

        try
        {
            if (!InstallerManager.IsDriveLetterAvailable(windowsDrive) ||
                !InstallerManager.IsDriveLetterAvailable(efiDrive))
            {
                DriveLetterError.Message = "A selected drive letter is now in use. Choose different unused letters.";
                DriveLetterError.IsOpen = true;
                RefreshDriveLetterChoices();
                return;
            }
        }
        catch (Exception ex)
        {
            DriveLetterError.Message = $"Could not check whether the selected drive letters are available: {ex.Message}";
            DriveLetterError.IsOpen = true;
            return;
        }

        DriveLetterError.IsOpen = false;
        StatusBar.IsOpen = false;
        ReviewSummary.Text =
            $"Media type: {(MediaTypeBox.SelectedIndex == 0 ? "ISO file" : "Mounted drive")}\n" +
            $"Image: {imagePath}\n" +
            $"Windows edition: {edition.Value} (index {edition.Key})\n" +
            $"Target disk number: {targetDiskNumber}\n" +
            $"Windows drive: {windowsDrive}\n" +
            $"EFI drive: {efiDrive}\n" +
            $"Drivers folder: {(string.IsNullOrWhiteSpace(DriversPathBox.Text) ? "None" : DriversPathBox.Text.Trim())}";
        ReviewCard.Visibility = Visibility.Visible;
    }

    private void TargetDisk_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _targetDiskNumber = (TargetDiskBox.SelectedItem as DiskOption)?.DiskNumber;
        ReviewCard.Visibility = Visibility.Collapsed;
    }

    private void DriveLetter_DropDownOpened(object sender, object e)
    {
        RefreshDriveLetterChoices();
    }

    private void DriveLetter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDestinationOptions || _isUpdatingDriveChoices)
        {
            return;
        }

        ComboBox changedBox = (ComboBox)sender;
        ComboBox otherBox = ReferenceEquals(changedBox, WindowsDriveBox)
            ? EfiDriveBox
            : WindowsDriveBox;
        string? selectedLetter = changedBox.SelectedItem as string;

        if (selectedLetter is not null)
        {
            try
            {
                if (!InstallerManager.IsDriveLetterAvailable(selectedLetter))
                {
                    DriveLetterError.Message = $"Drive {selectedLetter} is already in use. Select an unused drive letter.";
                    DriveLetterError.IsOpen = true;
                    RefreshDriveLetterChoices();
                    return;
                }
            }
            catch (Exception ex)
            {
                DriveLetterError.Message = $"Could not check drive {selectedLetter}: {ex.Message}";
                DriveLetterError.IsOpen = true;
                return;
            }
        }

        DriveLetterError.IsOpen = false;
        string? previousOtherSelection = otherBox.SelectedItem as string;

        _isUpdatingDriveChoices = true;
        try
        {
            otherBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, selectedLetter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            otherBox.SelectedItem = previousOtherSelection;
            otherBox.IsEnabled = otherBox.Items.Count > 0;
        }
        finally
        {
            _isUpdatingDriveChoices = false;
        }

        ReviewCard.Visibility = Visibility.Collapsed;
    }

    private void RefreshDriveLetterChoices()
    {
        string? windowsSelection = WindowsDriveBox.SelectedItem as string;
        string? efiSelection = EfiDriveBox.SelectedItem as string;
        HashSet<string> availableLetters;
        try
        {
            availableLetters = InstallerManager.GetAvailableDriveLetters()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            DriveLetterError.Message = $"Could not check drive letters: {ex.Message}";
            DriveLetterError.IsOpen = true;
            return;
        }

        _availableDriveLetters = availableLetters.OrderBy(letter => letter, StringComparer.OrdinalIgnoreCase).ToList();
        _isUpdatingDriveChoices = true;
        try
        {
            WindowsDriveBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, efiSelection, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            EfiDriveBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, windowsSelection, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            WindowsDriveBox.SelectedItem = windowsSelection;
            EfiDriveBox.SelectedItem = efiSelection;
            WindowsDriveBox.IsEnabled = WindowsDriveBox.Items.Count > 0;
            EfiDriveBox.IsEnabled = EfiDriveBox.Items.Count > 0;
        }
        finally
        {
            _isUpdatingDriveChoices = false;
        }

        string? occupiedSelection = new[] { windowsSelection, efiSelection }
            .FirstOrDefault(letter => letter is not null && !availableLetters.Contains(letter));
        if (occupiedSelection is not null)
        {
            DriveLetterError.Message = $"Drive {occupiedSelection} is now in use. Choose another drive letter.";
            DriveLetterError.IsOpen = true;
        }
        else
        {
            DriveLetterError.IsOpen = false;
        }
    }

    private async Task ScanImageEditionsAsync(string imagePath)
    {
        int scanVersion = ++_editionScanVersion;
        ImageEditionBox.ItemsSource = null;
        ImageEditionBox.IsEnabled = false;
        ImageSearchStatus.Text = "Scanning the image for available Windows editions...";

        try
        {
            Dictionary<int, string> editions = await Task.Run(
                () => InstallerManager.GetImageEditions(imagePath));
            if (scanVersion != _editionScanVersion ||
                !string.Equals(ImageFilesBox.SelectedItem as string, imagePath, StringComparison.Ordinal))
            {
                return;
            }

            if (editions.Count == 0)
            {
                ImageSearchStatus.Text = "No Windows editions were found in the selected image.";
                return;
            }

            ImageEditionBox.ItemsSource = editions.OrderBy(edition => edition.Key).ToArray();
            ImageEditionBox.SelectedIndex = 0;
            ImageEditionBox.IsEnabled = true;
            ImageSearchStatus.Text = $"Found {editions.Count} Windows edition(s).";
        }
        catch (Exception ex)
        {
            if (scanVersion == _editionScanVersion)
            {
                ShowValidationError($"Could not read Windows editions from this image: {ex.Message}");
            }
        }
    }

    private static string[] FindWindowsImages(string mediaRoot)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        return Directory.EnumerateFiles(mediaRoot, "*", options)
            .Where(path =>
                string.Equals(Path.GetFileName(path), "install.wim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "install.esd", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string MountIso(string isoPath)
    {
        string escapedPath = isoPath.Replace("'", "''", StringComparison.Ordinal);
        string script =
            "$ErrorActionPreference = 'Stop'; " +
            $"Mount-DiskImage -ImagePath '{escapedPath}' -PassThru | Out-Null; " +
            $"$volume = Get-DiskImage -ImagePath '{escapedPath}' | Get-Volume | Where-Object DriveLetter | Select-Object -First 1; " +
            "if ($null -eq $volume) { throw 'The ISO was mounted, but Windows did not assign it a drive letter.' }; " +
            "Write-Output ($volume.DriveLetter + ':')";

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PowerShell to mount the ISO.");
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        string output = outputTask.GetAwaiter().GetResult().Trim();
        string error = errorTask.GetAwaiter().GetResult().Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error) ? "Windows could not mount the ISO." : error);
        }

        return NormalizeDriveRoot(output);
    }

    private static string NormalizeDriveRoot(string value)
    {
        string drive = value.Trim();
        if (drive.Length == 1 && char.IsAsciiLetter(drive[0]))
        {
            drive += ':';
        }

        if (drive.Length is < 2 or > 3 ||
            !char.IsAsciiLetter(drive[0]) ||
            drive[1] != ':' ||
            (drive.Length == 3 && drive[2] != '\\'))
        {
            throw new ArgumentException("Enter a drive letter in the format E:.");
        }

        return char.ToUpperInvariant(drive[0]) + @":\";
    }

    private void ResetImageSearch()
    {
        _editionScanVersion++;
        ImageFilesBox.ItemsSource = null;
        ImageFilesBox.IsEnabled = false;
        ImageEditionBox.ItemsSource = null;
        ImageEditionBox.IsEnabled = false;
        ImageSearchStatus.Text = string.Empty;
        ReviewCard.Visibility = Visibility.Collapsed;
        StatusBar.IsOpen = false;
    }

    private void ShowValidationError(string message)
    {
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
        ReviewCard.Visibility = Visibility.Collapsed;
    }

    private sealed record DiskOption(int DiskNumber, string Model);

}
