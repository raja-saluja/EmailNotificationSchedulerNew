using Serilog;
using Serilog.Events;
using System;
using System.Configuration;
using System.IO;

namespace EmailNotificationNew
{

    public static class LoggerConfig
    {
        public static void Configure()
        {
            var basePath = ConfigurationManager.AppSettings["LogFilePath"];

            // Ensure directory exists
            var directory = Path.GetDirectoryName(basePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create file name with current date & time
            string fileName = $"EmailJob_{DateTime.Now:yyyyMMdd_HHmmss}.log";
            string fullPath = Path.Combine(directory, fileName);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Async(a => a.File(
                    fullPath,
                    shared: true, // allows multi-process access
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                ))
                .CreateLogger();
        }
    }



}
