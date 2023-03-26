using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WowsPlayerWatcher.Services;

namespace WowsPlayerWatcher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Trace.Listeners.Add(new TextWriterTraceListener("c:/temp/log.txt"));
            LoggerHelper.Factory = LoggerFactory.Create(
                builder => builder
                .AddDebug()
                .SetMinimumLevel(LogLevel.Debug)
                );
            ApplicationConfiguration.Initialize();
            Application.Run(new UI());
        }
    }
}