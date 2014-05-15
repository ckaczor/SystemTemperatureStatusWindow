using System.IO;
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

            var assembly = Assembly.GetExecutingAssembly();

            var path = Path.GetDirectoryName(assembly.Location);

            var logPath = path == null ? null : Path.Combine(path, "Logs");

            Tracer.Initialize(logPath, ScheduledTaskName, Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture), Environment.UserInteractive);

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

                            taskDefinition.Triggers.Add(new LogonTrigger { Delay = TimeSpan.FromSeconds(30) });
                            taskDefinition.Actions.Add(new ExecAction(assembly.Location));
                            taskDefinition.Settings.RestartInterval = TimeSpan.FromMinutes(1);
                            taskDefinition.Settings.RestartCount = 3;
                            taskDefinition.Settings.StartWhenAvailable = true;
                            taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;

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

                try
                {
                    using (_serviceHost = new ServiceHost(typeof(SystemTemperatureService)))
                    {
                        _serviceHost.Open();

                        var application = new Application();
                        application.DispatcherUnhandledException += HandleApplicationDispatcherUnhandledException;
                        application.Run();

                        _serviceHost.Close();
                    }
                }
                catch (Exception exception)
                {
                    Tracer.WriteException(exception);
                }
            }

            Tracer.WriteLine("Closing");
            Tracer.Dispose();
        }

        private static void HandleApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Tracer.WriteException(e.Exception);
        }
    }
}
