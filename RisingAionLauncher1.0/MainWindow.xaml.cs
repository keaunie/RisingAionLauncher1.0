using System;
using System.Collections.Generic;
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
            gameZip = Path.Combine(rootPath, "RisingAion5.8.zip");
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
                
                webClient.DownloadFileAsync(new Uri("https://drive.google.com/uc?export=download&id=1ky05w5L7LOrC3w5uQVLvBOhn-bW4QgGI"), gameZip, _onlineVersion);
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
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




public static class FileDownloader
{
    private const string GOOGLE_DRIVE_DOMAIN = "drive.google.com";
    private const string GOOGLE_DRIVE_DOMAIN2 = "https://drive.google.com";

    // Normal example: FileDownloader.DownloadFileFromURLToPath( "http://example.com/file/download/link", @"C:\file.txt" );
    // Drive example: FileDownloader.DownloadFileFromURLToPath( "http://drive.google.com/file/d/FILEID/view?usp=sharing", @"C:\file.txt" );
    public static FileInfo DownloadFileFromURLToPath(string url, string path)
    {
        if (url.StartsWith(GOOGLE_DRIVE_DOMAIN) || url.StartsWith(GOOGLE_DRIVE_DOMAIN2))
            return DownloadGoogleDriveFileFromURLToPath(url, path);
        else
            return DownloadFileFromURLToPath(url, path, null);
    }

    private static FileInfo DownloadFileFromURLToPath(string url, string path, WebClient webClient)
    {
        try
        {
            if (webClient == null)
            {
                using (webClient = new WebClient())
                {
                    webClient.DownloadFile(url, path);
                    return new FileInfo(path);
                }
            }
            else
            {
                webClient.DownloadFile(url, path);
                return new FileInfo(path);
            }
        }
        catch (WebException)
        {
            return null;
        }
    }

    // Downloading large files from Google Drive prompts a warning screen and
    // requires manual confirmation. Consider that case and try to confirm the download automatically
    // if warning prompt occurs
    private static FileInfo DownloadGoogleDriveFileFromURLToPath(string url, string path)
    {
        // You can comment the statement below if the provided url is guaranteed to be in the following format:
        // https://drive.google.com/uc?id=FILEID&export=download
        url = GetGoogleDriveDownloadLinkFromUrl(url);

        using (CookieAwareWebClient webClient = new CookieAwareWebClient())
        {
            FileInfo downloadedFile;

            // Sometimes Drive returns an NID cookie instead of a download_warning cookie at first attempt,
            // but works in the second attempt
            for (int i = 0; i < 2; i++)
            {
                downloadedFile = DownloadFileFromURLToPath(url, path, webClient);
                if (downloadedFile == null)
                    return null;

                // Confirmation page is around 50KB, shouldn't be larger than 60KB
                if (downloadedFile.Length > 60000)
                    return downloadedFile;

                // Downloaded file might be the confirmation page, check it
                string content;
                using (var reader = downloadedFile.OpenText())
                {
                    // Confirmation page starts with <!DOCTYPE html>, which can be preceeded by a newline
                    char[] header = new char[20];
                    int readCount = reader.ReadBlock(header, 0, 20);
                    if (readCount < 20 || !(new string(header).Contains("<!DOCTYPE html>")))
                        return downloadedFile;

                    content = reader.ReadToEnd();
                }

                int linkIndex = content.LastIndexOf("href=\"/uc?");
                if (linkIndex < 0)
                    return downloadedFile;

                linkIndex += 6;
                int linkEnd = content.IndexOf('"', linkIndex);
                if (linkEnd < 0)
                    return downloadedFile;

                url = "https://drive.google.com" + content.Substring(linkIndex, linkEnd - linkIndex).Replace("&amp;", "&");
            }

            downloadedFile = DownloadFileFromURLToPath(url, path, webClient);

            return downloadedFile;
        }
    }

    // Handles 3 kinds of links (they can be preceeded by https://):
    // - drive.google.com/open?id=FILEID
    // - drive.google.com/file/d/FILEID/view?usp=sharing
    // - drive.google.com/uc?id=FILEID&export=download
    public static string GetGoogleDriveDownloadLinkFromUrl(string url)
    {
        int index = url.IndexOf("id=");
        int closingIndex;
        if (index > 0)
        {
            index += 3;
            closingIndex = url.IndexOf('&', index);
            if (closingIndex < 0)
                closingIndex = url.Length;
        }
        else
        {
            index = url.IndexOf("file/d/");
            if (index < 0) // url is not in any of the supported forms
                return string.Empty;

            index += 7;

            closingIndex = url.IndexOf('/', index);
            if (closingIndex < 0)
            {
                closingIndex = url.IndexOf('?', index);
                if (closingIndex < 0)
                    closingIndex = url.Length;
            }
        }

        return string.Format("https://drive.google.com/uc?id={0}&export=download", url.Substring(index, closingIndex - index));
    }
}

// Web client used for Google Drive
public class CookieAwareWebClient : WebClient
{
    private class CookieContainer
    {
        Dictionary<string, string> _cookies;

        public string this[Uri url]
        {
            get
            {
                string cookie;
                if (_cookies.TryGetValue(url.Host, out cookie))
                    return cookie;

                return null;
            }
            set
            {
                _cookies[url.Host] = value;
            }
        }

        public CookieContainer()
        {
            _cookies = new D