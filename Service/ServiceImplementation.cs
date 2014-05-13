using Common.Debug;
using System;
using System.ServiceModel;
using System.ServiceProcess;
using SystemTemperatureService.Framework;

namespace SystemTemperatureService
{
    [WindowsService("SystemTemperatureStatus", DisplayName = "System Temperature Status", Description = "", StartMode = ServiceStartMode.Automatic, ServiceAccount = ServiceAccount.LocalSystem)]
    public class ServiceImplementation : IWindowsService
    {
        private ServiceHost _serviceHost;

        public void OnStart(string[] args)
        {
            using (new BeginEndTracer(GetType().Name))
            {
                try
                {
                    _serviceHost = new ServiceHost(typeof(SystemTemperatureService));
                    _serviceHost.Open();
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("ServiceImplementation.OnStart", exception);
                    throw;
                }
            }
        }

        public void OnStop()
        {
            using (new BeginEndTracer(GetType().Name))
            {
                try
                {
                    _serviceHost.Close();
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("ServiceImplementation.OnStop", exception);
                    throw;
                }
            }
        }

        public void OnPause() { }

        public void OnContinue() { }

        public void OnShutdown() { }

        public void Dispose() { }

        public void OnCustomCommand(int command) { }
    }
}
