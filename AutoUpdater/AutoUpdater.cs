using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace AutoUpdater
{
    class AutoUpdater
    {
        private static int MAJOR_VERSION = 1;
        private static int MINOR_VERSION = 0;
        private static int BUILD_VERSION = 1;
        private static int REVISION = 5;

        public static string VERSION =  String.Format("{0}.{1}.{2}.{3}", MAJOR_VERSION, MINOR_VERSION, BUILD_VERSION, REVISION); // MAJOR, 
        public static string AUTHOR = "RockyTV";

        public static string DATA_URL = "http://godarklight.info.tm/dmp/data/";
        public static string UPDATER_URL = "http://godarklight.info.tm:82/dmp/downloads/dmpupdater/DMPUpdater.exe";

        private static long checkInterval = 30 * 600000000L; // Check every 30 minutes for a new version.
        private static string buildDate;
        private static string buildVersion;
        private static string currentVersion;
        private static string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private static string throwError;

        private static bool keepRunning = true;

        private static string dmpUpdater = "DMPUpdater-development.exe";

        private static long lastCheckTime = DateTime.UtcNow.Ticks;

        static void Main(string[] args)
        {
            Console.Title = "AutoUpdater version " + VERSION + ", by " + AUTHOR;

            if (!File.Exists(Path.Combine(appDir, dmpUpdater)))
            {
                Log.Normal("Downloading DMPUpdater...");
                if (!DownloadUpdater())
                {
                    Log.Error(" DMPUpdater -> Failed!");
                    Log.Error("Error while downloading DMPUpdater: " + throwError);
                    keepRunning = false;
                }
                else
                {
                    Log.Debug(" DMPUpdater -> Success!");
                }
            }

            if (!File.Exists(Path.Combine(appDir, "DMPServer.exe")))
            {
                Log.Error("AutoUpdater wasn't installed correctly. Please install it on the server folder instead.");
                keepRunning = false;
            }

            while (keepRunning)
            {

                if (DateTime.UtcNow.Ticks > (lastCheckTime + checkInterval))
                {
                    lastCheckTime = DateTime.UtcNow.Ticks;
                    Log.Normal("Checking for a new version...");
                    if (!GetLatestVersion())
                    {
                        Log.Error("Error while checking for a new version: " + throwError);
                        keepRunning = false;
                    }
                    else
                    {
                        if (currentVersion != buildVersion && currentVersion != "")
                        {
                            Log.Normal("New version released! Version " + buildVersion.Substring(0, 7) + ", built on " + buildDate);
                            Log.Debug("Shutting down server and launching DMPUpdater...");
                            Thread.Sleep(1000);
                            UpdateServer();
                        }
                        else
                        {
                            Log.Normal("No new version found.");
                        }
                    }
                }
            }

            if (!keepRunning)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
        }

        // Update server
        public static void UpdateServer()
        {
            Process.Start(dmpUpdater, "-b");
            RestartServer();
        }

        // Restart server
        public static void RestartServer()
        {
            /* Logic of these loops:
             * 
             * Do a check for every DMPServer running (handy for multiple servers in one machine)
             * If there is a DMPServer running and its Path is the same as our program, kill it.
             * Wait for DMPUpdater to finish, then run the server again.
             * 
             */
            foreach (Process proc in Process.GetProcessesByName("DMPServer"))
            {
                if (ProcessExecutablePath(proc) == Path.Combine(appDir, "DMPServer.exe"))
                {
                    Log.Normal("Shutting down server...");
                    try
                    {
                        proc.Kill();
                        Log.Debug("Server was shut down.");
                    }
                    catch (Exception e)
                    {
                        throwError = e.Message;
                        Log.Error("Error while shutting down server: " + throwError);
                        keepRunning = false;
                    }
                    foreach (Process updt in Process.GetProcessesByName("DMPUpdater-development"))
                    {
                        if (ProcessExecutablePath(updt) == Path.Combine(appDir, dmpUpdater))
                        {
                            Log.Normal("Updating server...");
                            updt.WaitForExit();
                            Log.Normal("Server was updated!");

                            Log.Debug("Starting server...");
                            try
                            {
                                Process.Start("DMPServer.exe");
                                Log.Debug("Server was started.");
                            }
                            catch (Exception e)
                            {
                                throwError = e.Message;
                                Log.Error("Error while starting DMPServer: " + throwError);
                                keepRunning = false;
                            }
                        }
                    }
                }
            }
        }

        // Download DMPUpdater
        public static bool DownloadUpdater()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(UPDATER_URL, dmpUpdater);
                }
            }
            catch (Exception e)
            {
                throwError = e.Message;
                return false;
            }
            return true;
        }

        // Download latest version
        public static bool GetLatestVersion()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    currentVersion = buildVersion; // This means that the current checked version should always be buildVersion.

                    buildVersion = wc.DownloadString(DATA_URL + "develtag");
                    buildDate = wc.DownloadString(DATA_URL + "develbuilddate");
                }
            }
            catch (Exception e)
            {
                throwError = e.Message;
                return false;
            }
            return true;
        }

        static private string ProcessExecutablePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject item in searcher.Get())
                {
                    object id = item["ProcessID"];
                    object path = item["ExecutablePath"];

                    if (path != null && id.ToString() == process.Id.ToString())
                    {
                        return path.ToString();
                    }
                }
            }

            return "";
        }

    }
}
