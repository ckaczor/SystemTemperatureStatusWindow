using System.ServiceModel;
using Common.Debug;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace SystemTemperatureService
{
    public class Program
    {
        private const string ScheduledTaskName = "SystemTemperatureService";

        public static Dispatcher MainDispatcher { get; set; }

        private static ServiceHost _serviceHost;

        static void Main(string[] args)
        {
            MainDispatcher = Dispatcher.CurrentDispatcher;

            Tracer.Initialize(null, null, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), Environment.UserInteractive);

            if (args.Contains("-install", StringComparer.InvariantCultureIgnoreCase))
            {
                Tracer.WriteLine("Starting install...");

                try
                {
                    using (var taskService = new TaskService())
                    {
                        var existingTask = taskService.FindTask(ScheduledTaskName);

                        if (existingTask == null)
                        {
                            var taskDefinition = taskService.NewTask();
                            taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                            taskDefinition.Triggers.Add(new LogonTrigger());
                            taskDefinition.Actions.Add(new ExecAction(Assembly.GetExecutingAssembly().Location));

                            taskService.RootFolder.RegisterTaskDefinition(ScheduledTaskName, taskDefinition);
                        }

                        existingTask = taskService.FindTask(ScheduledTaskName);
                        existingTask.Run();
                    }
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("Install", exception);
                }

                Tracer.WriteLine("Install complete");
            }
            else if (args.Contains("-uninstall", StringComparer.InvariantCultureIgnoreCase))
            {
                Tracer.WriteLine("Starting uninstall...");

                try
                {
                    using (var taskService = new TaskService())
                        taskService.RootFolder.DeleteTask(ScheduledTaskName, false);
                }
                catch (Exception exception)
                {
                    Tracer.WriteException("Uninstall", exception);
                }

                Tracer.WriteLine("Uninstall complete");
            }
            else
            {
                Tracer.WriteLine("Starting");

                using (_serviceHost = new ServiceHost(typeof(SystemTemperatureService)))
                {
                    _serviceHost.Open();

                    var application = new Application();
                    application.Run();

                    _serviceHost.Close();
                }
            }
        }
    }
}
