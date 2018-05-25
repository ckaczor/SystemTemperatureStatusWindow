using FloatingStatusWindowLibrary;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using SystemTemperatureStatusWindow.Options;
using SystemTemperatureStatusWindow.Properties;
using SystemTemperatureStatusWindow.SystemTemperatureService;
using Common.Wpf.Windows;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace SystemTemperatureStatusWindow
{
    public class WindowSource : IWindowSource, IDisposable
    {
        private const string ScheduledTaskName = "SystemTemperatureService";

        private readonly FloatingStatusWindow _floatingStatusWindow;
        private readonly Timer _refreshTimer;
        private readonly Dispatcher _dispatcher;

        private CategoryWindow _optionsWindow;

        internal WindowSource()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            try
            {
                using (var taskService = new TaskService())
                {
                    var existingTask = taskService.FindTask(ScheduledTaskName);

                    if (existingTask == null)
                    {
                        var assembly = Assembly.GetExecutingAssembly();

                        var path = Path.GetDirectoryName(assembly.Location);

                        if (path != null)
                        {
                            var fileName = Path.Combine(path, "Service", "SystemTemperatureService.exe");

                            Process.Start(fileName, "-install");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }

            _floatingStatusWindow = new FloatingStatusWindow(this);
            _floatingStatusWindow.SetText(Resources.Loading);

            _refreshTimer = new Timer(Settings.Default.UpdateInterval) { AutoReset = false };
            _refreshTimer.Elapsed += HandleTimerElapsed;


            System.Threading.Tasks.Task.Factory.StartNew(UpdateApp).ContinueWith(task => StartUpdate(task.Result.Result));
        }

        private void StartUpdate(bool updateRequired)
        {
            if (updateRequired)
                return;

            System.Threading.Tasks.Task.Factory.StartNew(() => _refreshTimer.Start());
        }

        private async Task<bool> UpdateApp()
        {
            return await UpdateCheck.CheckUpdate(HandleUpdateStatus);
        }

        private void HandleUpdateStatus(UpdateCheck.UpdateStatus status, string message)
        {
            if (status == UpdateCheck.UpdateStatus.None)
                message = Resources.Loading;

            _dispatcher.Invoke(() => _floatingStatusWindow.SetText(message));
        }

        private void HandleTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Update();
        }

        private void Update()
        {
            try
            {
                using (var client = new SystemTemperatureServiceClient())
                {
                    var builder = new StringBuilder();

                    var deviceList = client.GetDeviceList();

                    foreach (var device in deviceList)
                    {
                        string hardwareTag = string.Empty;

                        switch (device.Type)
                        {
                            case DeviceType.Cpu:
                                hardwareTag = Resources.CPU;
                                break;

                            case DeviceType.Gpu:
                                hardwareTag = Resources.GPU;
                                break;

                            case DeviceType.Hdd:
                                string id = device.Id;

                                hardwareTag = string.Format(Resources.HD, id.Substring(id.LastIndexOf("/", StringComparison.Ordinal) + 1));

                                break;
                        }

                        if (!string.IsNullOrWhiteSpace(hardwareTag))
                        {
                            var averageValue = device.Temperature;

                            string color = "green";

                            if (averageValue > Settings.Default.AlertLevel)
                                color = "red";
                            else if (averageValue > Settings.Default.WarningLevel)
                                color = "yellow";

                            double averageDisplay;
                            string suffix;

                            if (Settings.Default.DisplayF)
                            {
                                averageDisplay = ((9.0 / 5.0) * averageValue) + 32;
                                suffix = Resources.SuffixF;
                            }
                            else
                            {
                                averageDisplay = averageValue;
                                suffix = Resources.SuffixC;
                            }

                            if (builder.Length > 0)
                                builder.AppendLine();

                            builder.AppendFormat(Resources.DisplayLineTemplate, hardwareTag, averageDisplay, color, suffix);
                        }
                    }

                    UpdateText(builder.ToString());
                }
            }
            catch (Exception exception)
            {
                UpdateText(exception.Message);
            }
        }

        public void Dispose()
        {
            _refreshTimer.Dispose();

            _floatingStatusWindow.Save();
            _floatingStatusWindow.Dispose();
        }

        private void UpdateText(string text)
        {
            _dispatcher.Invoke(() => _floatingStatusWindow.SetText(text));

            _refreshTimer.Start();
        }

        public void ShowAbout()
        {
        }

        public string Name => "System Temperature";

        public System.Drawing.Icon Icon => Resources.ApplicationIcon;

        public void ShowSettings()
        {
            var panels = new List<CategoryPanel>
            {
                new GeneralOptionsPanel(),
                new AboutOptionsPanel()
            };

            if (_optionsWindow == null)
            {
                _optionsWindow = new CategoryWindow(null, panels, Resources.ResourceManager, "OptionsWindow");
                _optionsWindow.Closed += (o, args) => { _optionsWindow = null; };
            }

            var dialogResult = _optionsWindow.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                Settings.Default.Save();

                Refresh();
            }
        }

        public void Refresh()
        {
            Update();
        }

        public bool HasSettingsMenu => true;
        public bool HasRefreshMenu => true;
        public bool HasAboutMenu => false;

        public string WindowSettings
        {
            get => Settings.Default.WindowSettings;
            set
            {
                Settings.Default.WindowSettings = value;
                Settings.Default.Save();
            }
        }
    }
}
