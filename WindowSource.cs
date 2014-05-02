using FloatingStatusWindowLibrary;
using OpenHardwareMonitor.Hardware;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using SystemTemperatureStatusWindow.Properties;

namespace SystemTemperatureStatusWindow
{
    public class WindowSource : IWindowSource, IDisposable
    {
        private readonly FloatingStatusWindow _floatingStatusWindow;
        private readonly BackgroundWorker _backgroundWorker;

        internal WindowSource()
        {
            _floatingStatusWindow = new FloatingStatusWindow(this);
            _floatingStatusWindow.SetText(Resources.Loading);

            _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            _backgroundWorker.ProgressChanged += HandleBackgroundWorkerProgressChanged;
            _backgroundWorker.DoWork += HandleBackgroundWorkerDoWork;
            _backgroundWorker.RunWorkerCompleted += HandleBackgroundWorkerRunWorkerCompleted;
            _backgroundWorker.RunWorkerAsync();
        }

        public void Dispose()
        {
            _backgroundWorker.CancelAsync();

            _floatingStatusWindow.Save();
            _floatingStatusWindow.Dispose();
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

        void HandleBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = (BackgroundWorker) sender;

            var computer = new Computer { HDDEnabled = true, FanControllerEnabled = false, GPUEnabled = true, MainboardEnabled = true, CPUEnabled = true };
            computer.Open();

            while (!backgroundWorker.CancellationPending)
            {
                var builder = new StringBuilder();

                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();

                    var averageValue = hardware.Sensors.Where(sensor => sensor.SensorType == SensorType.Temperature).Average(sensor => sensor.Value);

                    if (averageValue.HasValue)
                    {
                        string hardwareTag = string.Empty;

                        switch (hardware.HardwareType)
                        {
                            case HardwareType.CPU:
                                hardwareTag = Resources.CPU;
                                break;

                            case HardwareType.GpuAti:
                            case HardwareType.GpuNvidia:
                                hardwareTag = Resources.GPU;
                                break;

                            case HardwareType.HDD:
                                string id = hardware.Identifier.ToString();
                                hardwareTag = string.Format(Resources.HD, id.Substring(id.LastIndexOf("/", StringComparison.Ordinal) + 1));
                                break;
                        }

                        if (!string.IsNullOrWhiteSpace(hardwareTag))
                        {
                            string color = "green";

                            if (averageValue > Settings.Default.AlertLevel)
                                color = "red";
                            else if (averageValue.Value > Settings.Default.WarningLevel)
                                color = "yellow";

                            double averageDisplay;
                            string suffix;

                            if (Settings.Default.DisplayF)
                            {
                                averageDisplay = ((9.0 / 5.0) * averageValue.Value) + 32;
                                suffix = Resources.SuffixF;
                            }
                            else
                            {
                                averageDisplay = averageValue.Value;
                                suffix = Resources.SuffixC;
                            }

                            if (builder.Length > 0)
                                builder.AppendLine();

                            builder.AppendFormat(Resources.DisplayLineTemplate, hardwareTag, averageDisplay, color, suffix);
                        }
                    }
                }

                backgroundWorker.ReportProgress(0, builder.ToString());

                Thread.Sleep(Settings.Default.UpdateInterval);
            }

            computer.Close();
        }

        void HandleBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _floatingStatusWindow.SetText((string) e.UserState);
        }

        void HandleBackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }
    }
}
