using System;
using System.IO;

namespace AutoUpdater
{
    public class Log
    {
        private static string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("AutoUpdater", "logs"));
        private static string logFile = Path.Combine(logDir, "autoupdater " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log");

        public enum LogLevels
        {
            DEBUG,
            INFO,
            ERROR
        }

        private static void WriteLog(LogLevels level, string message)
        {
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            string output = "[" + DateTime.Now.ToString("HH:mm:ss") + "][" + level.ToString() + "] : " + message;
            Console.WriteLine(output);
            
            try
            {
                File.AppendAllText(logFile, output + Environment.NewLine);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Error writing to log file!, Exception: " + e);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public static void Debug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            WriteLog(LogLevels.DEBUG, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Normal(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteLog(LogLevels.INFO, message);
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            WriteLog(LogLevels.ERROR, message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
