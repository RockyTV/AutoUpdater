using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using DarkMultiPlayerServer;

namespace AutoUpdater
{
    public class Common
    {
        public static string VERSION =  "1.0.0.0";
        public static string AUTHOR = "RockyTV";

        public static string DATA_URL = "http://godarklight.info.tm/dmp/data/";
        public static string UPDATER_URL = "http://godarklight.info.tm:82/dmp/downloads/dmpupdater/DMPUpdater.exe";

        private static long checkInterval = 1 * 600000000L;
        private static string buildDate;
        private static string buildVersion;
        private static string currentVersion;
        private static string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private static string throwError;

        private static string dmpUpdater = "DMPUpdater-development.exe";

        private static long lastCheckTime = DateTime.UtcNow.Ticks;

        // Do the check for a new version
        public static void DoCheck()
        {
            if (!File.Exists(Path.Combine(appDir, dmpUpdater)))
            {
                DarkLog.Normal("[AutoUpdater] DMPUpdater not found. Downloading...");
                if (!DownloadUpdater())
                {
                    DarkLog.Error("[AutoUpdater] Failed to download DMPUpdater.");
                    DarkLog.Error("[AutoUpdater] " + throwError);
                }
                else
                {
                    DarkLog.Normal("[AutoUpdater] DMPUpdater was downloaded.");
                }
            }

            if (DateTime.UtcNow.Ticks > (lastCheckTime + checkInterval))
            {
                lastCheckTime = DateTime.UtcNow.Ticks;
                DarkLog.Normal("[AutoUpdater] Checking for a new version...");
                if (!GetLatestVersion())
                {
                    DarkLog.Error("[AutoUpdater] Failed to check for a new version.");
                    DarkLog.Error("[AutoUpdater] " + throwError);
                }
                else
                {
                    if (currentVersion != buildVersion)
                    {
                        DarkLog.Normal("[AutoUpdater] New version released! Version " + buildVersion.Substring(0, 7));
                        DarkLog.Normal("[AutoUpdater] Shutting down server and launching DMPUpdater...");
                        Server.ShutDown("Updating server");
                        Thread.Sleep(1000);
                        UpdateServer();
                    }
                    else
                    {
                        DarkLog.Normal("[AutoUpdater] No new version found.");
                    }
                }
            }
        }

        // Update server
        public static void UpdateServer()
        {
            Process.Start(dmpUpdater);
            RestartServer();
        }

        // Restart server
        public static void RestartServer()
        {
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DMPServer.exe"));
            Thread.Sleep(1);
            Environment.Exit(-1);
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
                    if (currentVersion != "")
                        currentVersion = buildVersion;

                    buildVersion = wc.DownloadString(DATA_URL + "develtag");
                    buildDate = wc.DownloadString(DATA_URL + "develbuilddate");
                    currentVersion = buildVersion;
                }
            }
            catch (Exception e)
            {
                throwError = e.Message;
                return false;
            }
            return true;
        }
    }
}
