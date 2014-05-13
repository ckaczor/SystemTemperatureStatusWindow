using FloatingStatusWindowLibrary;
using System;
using System.Text;
using System.Timers;
using System.Windows.Threading;
using SystemTemperatureStatusWindow.Properties;
using SystemTemperatureStatusWindow.SystemTemperatureService;

namespace SystemTemperatureStatusWindow
{
    public class WindowSource : IWindowSource, IDisposable
    {
        private readonly FloatingStatusWindow _floatingStatusWindow;
        private readonly Timer _refreshTimer;
        private readonly Dispatcher _dispatcher;

        internal WindowSource()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;

            _floatingStatusWindow = new FloatingStatusWindow(this);
            _floatingStatusWindow.SetText(Resources.Loading);

            _refreshTimer = new Timer(Settings.Default.UpdateInterval) { AutoReset = false };
            _refreshTimer.Elapsed += HandleTimerElapsed;
            _refreshTimer.Start();
        }

        private void HandleTimerElapsed(object sender, ElapsedEventArgs e)
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

        public string Name
        {
            get { return "System Temperature"; }
        }

        public System.Drawing.Icon Icon
        {
            get { return Resources.ApplicationIcon; }
        }

        public string WindowSettings
        {
            get
            {
                return Settings.Default.WindowSettings;
            }
            set
            {
                Settings.Default.WindowSettings = value;
                Settings.Default.Save();
            }
        }
    }
}
