using Common.Debug;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceProcess;
using SystemTemperatureService.Framework;

namespace SystemTemperatureService
{
    class Program
    {
        static void Main(string[] args)
        {
            Tracer.Initialize(null, null, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), Environment.UserInteractive);

            if (args.Contains("-install", StringComparer.InvariantCultureIgnoreCase))
            {
                Tracer.WriteLine("Starting install...");

                try
                {
                    WindowsServiceInstaller.RuntimeInstall<ServiceImplementation>();
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("Service install", exception);
                }

                Tracer.WriteLine("Install complete");
            }
            else if (args.Contains("-uninstall", StringComparer.InvariantCultureIgnoreCase))
            {
                Tracer.WriteLine("Starting uninstall...");

                try
                {
                    WindowsServiceInstaller.RuntimeUnInstall<ServiceImplementation>();
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("Service uninstall", exception);
                }

                Tracer.WriteLine("Uninstall complete");
            }
            else
            {
                Tracer.WriteLine("Starting service");

                var implementation = new ServiceImplementation();

                if (Environment.UserInteractive)
                    ConsoleHarness.Run(args, implementation);
                else
                    ServiceBase.Run(new WindowsServiceHarness(implementation));
            }

        }
    }
}
