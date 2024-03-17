using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Automation;

namespace RisingAionLauncher1._0
{
    enum LauncherStatus { 
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;



        private LauncherStatus _status;

        internal LauncherStatus Status {
            get => _status;
            set { 
                _status = value;
                switch(_status)
                {
                    case LauncherStatus.ready:PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:PlayButton.Content = "Downloading Update";
                        break;
                    default: break;


                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "RisingAion5.8.rar");
            gameExe = Path.Combine(rootPath, "RisingAion5.8", "Aion_Start.bat");
        }

        private void CheckForUpdates() { 
            Version localVersion = new Version(File.ReadAllText(versionFile));
            versionText.Text = localVersion.ToString();

            try {
                WebClient webClient = new WebClient();
                Version onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1xysf_JXM7Axf9IIHMh81IcMMLUQiPtQv"));
                if (onlineVersion.isDifferentThan(localVersion))
                {
                    InstallGameFiles(false, onlineVersion);
                }
                else {
                    Status = LauncherStatus.ready;
                }
            }catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error checking for game updates: {ex}");
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;

                }
                else {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://drive.google.com/uc?export=download&id=1xysf_JXM7Axf9IIHMh81IcMMLUQiPtQv"));
                }
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1qztWKbsDrCrfB69mM5WnyYmHyjdE17TD"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);
                File.WriteAllText(versionFile, onlineVersion);
                versionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error Finishing Download: {ex}");
            }
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready) {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "RisingAion5.8");
                Process.Start(startInfo);
                Close();
            }else if(Status == LauncherStatus.failed) {
                CheckForUpdates();

            }
        }
    }

    struct Version {
        internal static Version zero = new Version(0,0,0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor) { 
            major = _major;
            minor = _minor;
            subMinor = _subMinor;

        }

        internal Version(string _version)
        {
            string[] _versionStrings = _version.Split('.');
            if (_versionStrings.Length !=3) {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(_versionStrings[0]);
            minor = short.Parse(_versionStrings[1]);
            subMinor = short.Parse(_versionStrings[2]);
        }

        internal bool isDifferentThan(Version _otherVersion) {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else {
                    if (subMinor != _otherVersion.subMinor) { 
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
