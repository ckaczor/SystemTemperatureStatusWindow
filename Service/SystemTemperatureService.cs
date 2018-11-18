using OpenHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SystemTemperatureService
{
    public class SystemTemperatureService : ISystemTemperatureService
    {
        private static Computer _computer;

        public List<Device> GetDeviceList()
        {
            if (_computer == null)
            {
                _computer = new Computer { HDDEnabled = true, FanControllerEnabled = false, GPUEnabled = true, MainboardEnabled = true, CPUEnabled = true };
                _computer.Open();
            }

            var deviceList = new List<Device>();

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                var averageValue = hardware.Sensors.Where(sensor => sensor.SensorType == SensorType.Temperature).Average(sensor => sensor.Value);

                if (averageValue.HasValue)
                {
                    Device device = null;

                    switch (hardware.HardwareType)
                    {
                        case HardwareType.CPU:
                            device = new Device { Type = DeviceType.Cpu };
                            break;

                        case HardwareType.GpuAti:
                        case HardwareType.GpuNvidia:
                            device = new Device { Type = DeviceType.Gpu };
                            break;

                        case HardwareType.HDD:
                            device = new Device { Type = DeviceType.Hdd };
                            break;
                    }

                    if (device != null)
                    {
                        device.Id = hardware.Identifier.ToString();
                        device.Temperature = averageValue.Value;
                    }

                    deviceList.Add(device);
                }
            }

            return deviceList;
        }

        public void Shutdown()
        {
            _computer.Close();

            Program.MainDispatcher.Invoke(Application.Current.Shutdown);
        }
    }
}
