using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinFS
{

    public class Logger
    {
        // Method to write a log entry
        public static void WriteLog(string message)
        {
            // Define the log file path - using relative path assuming the executable is running from the solution folder
            string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cavlog.log");

            // Construct the log message with current date and time
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            // Write the log message to the file, appending to it if it already exists
            File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        }

    }

}
