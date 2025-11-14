using System.Configuration;
using System.Data;
using System.Windows;
using Updatum;
using MessageBox = System.Windows.MessageBox;

namespace WindowsEdgeLight;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    internal static readonly UpdatumManager AppUpdater = new("shanselman", "WindowsEdgeLight")
    {
        // Asset pattern matches: WindowsEdgeLight-v0.6-win-x64.exe or .zip
        AssetRegexPattern = $"WindowsEdgeLight.*win-x64",
        // Prefer ZIP files for easier updates
        AssetExtensionFilter = "zip",
        // For MSI installer, show basic UI during installation
        InstallUpdateWindowsInstallerArguments = "/qb",
    };

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Check for updates asynchronously
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            // Wait a bit before checking to let the main window load
            await Task.Delay(2000);

            var updateFound = await AppUpdater.CheckForUpdatesAsync();

            if (!updateFound) return;

            // Show update dialog on UI thread
            await Dispatcher.InvokeAsync(async () =>
            {
                var release = AppUpdater.LatestRelease!;
                var changelog = AppUpdater.GetChangelog(true) ?? "No release notes available.";

                var dialog = new UpdateDialog(release.TagName, changelog);
                var result = dialog.ShowDialog();

                if (dialog.Result == UpdateDialog.UpdateDialogResult.Download)
                {
                    await DownloadAndInstallUpdateAsync();
                }
            });
        }
        catch (Exception ex)
        {
            // Silently fail - don't interrupt the user experience
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    private async Task DownloadAndInstallUpdateAsync()
    {
        try
        {
            var progressDialog = new DownloadProgressDialog(AppUpdater);
            progressDialog.Show();

            var downloadedAsset = await AppUpdater.DownloadUpdateAsync();

            progressDialog.Close();

            if (downloadedAsset == null)
            {
                MessageBox.Show("Failed to download the update. Please try again later.",
                    "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ask for confirmation before installing
            var confirmResult = MessageBox.Show(
                "The update has been downloaded. The application will now close and install the update.\n\nDo you want to continue?",
                "Install Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.Yes)
            {
                await AppUpdater.InstallUpdateAsync(downloadedAsset);
                // If installation succeeds, the app will be terminated
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to download or install update: {ex.Message}",
                "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

