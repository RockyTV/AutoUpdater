using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Management;
using System.Threading;
using Newtonsoft.Json;
using System.Reflection;
using System.ComponentModel;

namespace AutoUpdater
{
    class AutoUpdater
    {
        public static string EXE_NAME = AppDomain.CurrentDomain.FriendlyName;
        public static string VERSION = "release";
        public static string JOB_FOLDER;
        public static string MD5_INDEX;
        public static string OBJECTS_FOLDER;
        public static string API_URL;

        private static long checkInterval = 1 * 600000000L; // Check every 30 minutes for a new version.
        private static string buildDate;
        private static string buildVersion;
        private static string currentVersion;
        private static string appDir = AppDomain.CurrentDomain.BaseDirectory;
        private static string throwError;

        private static bool batchMode;
        private static bool skipIntro;

        private static List<string> validVersions = new List<string>();

        private static string[] fileIndex;
        private static Dictionary<string, string> filesList = new Dictionary<string, string>();

        //private static string backupsDir = Path.Combine(appDir, currentVersion);

        private static bool keepRunning = true;

        private static long lastCheckTime = DateTime.UtcNow.Ticks;

        static void ShowIntro()
        {
            Console.WriteLine("AutoUpdater is an external program that auto updates your server every time a new development update is released.");
            Console.WriteLine();
            Console.WriteLine("Coded by Alexandre Oliveira, aka RockyTV.");
            Console.WriteLine();
            Console.WriteLine("Many thanks to Christopher Andrews, aka godarklight, for DMPUpdater being open-source.");

            Thread.Sleep(5000);
            Console.Clear();
        }

        static void Main(string[] args)
        {
            validVersions.Add("release");
            validVersions.Add("development");
            validVersions.Add("unstable");

            if (args.Length > 0 && args.Length < 3)
            {
                if (args.Length == 2)
                {
                    batchMode = (args[0] == "-b" || args[0] == "--batch" || args[0] == "-batch" || args[1] == "-b" || args[1] == "--batch" || args[1] == "-batch") ? true : false;
                    skipIntro = (args[0] == "-skip" || args[0] == "--skip" || args[1] == "-skip" || args[1] == "--skip") ? true : false;
                }
                else if (args.Length == 1)
                {
                    batchMode = (args[0] == "-b" || args[0] == "--batch" || args[0] == "-batch") ? true : false;
                    skipIntro = (args[0] == "-skip" || args[0] == "--skip") ? true : false;
                }
            }

            Console.Title = "AutoUpdater version " + Utils.VERSION + ", by " + Utils.AUTHOR;


            if (!skipIntro)
            {
                ShowIntro();
            }

            if (!File.Exists(Path.Combine(appDir, "DMPServer.exe")))
            {
                Log.Error("AutoUpdater wasn't installed correctly. Please install it on the server folder instead.");
                keepRunning = false;
            }

            // Name logic checking, thanks darklight!
            string exeName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
            if (!exeName.Contains("-"))
            {
                if (exeName == "AutoUpdater")
                {
                    VERSION = "release";
                }
                else
                {
                    Log.Error("Badly formatted version. Valid versions are: " + string.Join(", ", validVersions.ToArray()));
                    keepRunning = false;
                }
            }
            else
            {
                string ver = exeName.Remove(0, exeName.LastIndexOf("-") + 1).ToLowerInvariant();
                if (validVersions.Contains(ver))
                {
                    VERSION = ver;
                }
                else
                {
                    Log.Error("Badly formatted version. Valid versions are: " + string.Join(", ", validVersions.ToArray()));
                    keepRunning = false;
                }
            }

            // Set our variables
            JOB_FOLDER = "http://dmp-jenkins.cf/job/" + VERSION + "/";
            MD5_INDEX = JOB_FOLDER + "lastSuccessfulBuild/artifact/server/server.txt";
            OBJECTS_FOLDER = JOB_FOLDER + "lastSuccessfulBuild/artifact/server/";
            API_URL = JOB_FOLDER + "/api/json";

            string os = Environment.OSVersion.Platform.ToString();
            if ((os == "Win32NT" || os == "Win32S" || os == "Win32Windows" || os == "WinCE") && (!Utils.IsRunningAsAdministrator()))
            {
                // Restart the program in admin mode. If we don't run the program under admin mode on Windows, it will crash if your server has the http port enabled or if it is running under admin mode.
                Log.Debug("OS is Windows and Application is not running as Administrator.");
                Log.Debug("Prompting the user to switch to administrator mode.");

                try
                {

                    ProcessStartInfo startInfo = new ProcessStartInfo(EXE_NAME) { Verb = "runas" };
                    startInfo.Arguments = "-skip";
                    Process.Start(startInfo);

                    Environment.Exit(0); // Terminate the current app.
                }
                catch (Win32Exception e)
                {
                    Log.Debug("User did not switch to administrator mode. Quitting.");
                    keepRunning = false;
                }
            }

            while (keepRunning)
            {

                if (DateTime.UtcNow.Ticks > (lastCheckTime + checkInterval))
                {
                    lastCheckTime = DateTime.UtcNow.Ticks;
                    Log.Normal("Grabbing latest commit...");
                    if (!GetLatestVersion())
                    {
                        Log.Error("Failed to grab latest commit: " + throwError);
                        keepRunning = false;
                    }
                    else
                    {
                        if (currentVersion != buildVersion)
                        {
                            Log.FriendlyWarning("New Jenkins build! Build #" + buildVersion);
                            Log.Normal("Downloading file index...");
                            if (!GetFileIndex())
                            {
                                Log.Error("Failed to download file index: " + throwError);
                                keepRunning = false;
                            }

                            Log.Normal("Parsing file index...");
                            if (!ParseFileIndex())
                            {
                                Log.Error("Error while parsing file index: " + throwError);
                                keepRunning = false;
                            }
                        }
                    }

                    Thread.Sleep(500);
                }
            }

            if (!keepRunning && !batchMode)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit... ");
                Console.ReadKey();
            }
        }

        // Shutdown the server
        public static bool ShutdownServer()
        {
            foreach (Process proc in Process.GetProcessesByName("DMPServer"))
            {
                if (Utils.ProcessExecutablePath(proc) == Path.Combine(appDir, "DMPServer.exe"))
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception e)
                    {
                        throwError = e.Message;
                        return false;
                    }
                }
            }
            return true;
        }

        // Restart server
        public static bool StartServer()
        {
            try
            {
                Process.Start("DMPServer.exe");
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

                    string downloadedJson = wc.DownloadString(API_URL);
                    JenkinsAPI parsedJson = JsonConvert.DeserializeObject<JenkinsAPI>(downloadedJson);

                    buildVersion = parsedJson.lastSuccessfulBuild["number"];
                }
            }
            catch (Exception e)
            {
                throwError = e.Message;
                return false;
            }
            return true;
        }

        public static bool GetFileIndex()
        {
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    fileIndex = Encoding.UTF8.GetString(webClient.DownloadData(MD5_INDEX)).Split(new string[] { "\n" }, StringSplitOptions.None);
                }
                catch (Exception e)
                {
                    throwError = e.Message;
                    return false;
                }
            }
            return true;
        }

        public static bool ParseFileIndex()
        {
            bool needsUpdating = false;
            foreach (string fileEntry in fileIndex)
            {
                if (fileEntry.Contains(" "))
                {
                    string shaHash = fileEntry.Remove(fileEntry.IndexOf(" ") + 1).Trim();
                    string file = fileEntry.Remove(0, fileEntry.IndexOf(" ")).Trim();
                    Console.WriteLine("File: " + file);
                    Console.WriteLine("Hash: " + shaHash);
                    if (!IsFileUpToDate(file, shaHash))
                    {
                        needsUpdating = true;
                        if (!filesList.ContainsKey(file))
                        {
                            filesList.Add(file, shaHash);
                        }
                    }
                }
            }

            if (needsUpdating)
            {
                if (!ShutdownServer())
                {
                    Log.Error("Error while shutting down server: " + throwError);
                    keepRunning = false;
                }
                else
                {
                    Log.Debug("Server was shutdown.");
                }

                foreach (KeyValuePair<string, string> key in filesList)
                {
                    string file = key.Key;
                    string shaHash = key.Value;

                    Log.Normal("Updating file " + file + " ");
                    if (!UpdateFile(file, shaHash))
                    {
                        Log.Error("Failed to update file " + file + ": " + throwError);
                        return false;
                    }
                    else
                    {
                        Log.Normal(file + " was updated!");
                    }
                }

                filesList.Clear();

                if (!StartServer())
                {
                    Log.Error("Error while starting server: " + throwError);
                    keepRunning = false;
                }
                else
                {
                    Log.Debug("Server started.");
                }
            }
            else
            {
                Log.Normal("No files need updating.");
            }


            return true;
        }

        private static bool IsFileUpToDate(string file, string shaHash)
        {
            if (!File.Exists(Path.Combine(appDir, file)))
            {
                return false;
            }

            using (FileStream fs = new FileStream(Path.Combine(appDir, file), FileMode.Open, FileAccess.Read))
            {
                using (MD5 md5 = MD5.Create())
                {
                    string fileSha = BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
                    if (shaHash != fileSha)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool UpdateFile(string file, string shaHash)
        {
            if (File.Exists(Path.Combine(appDir, file)))
            {
                File.Delete(Path.Combine(appDir, file));
            }
            using (FileStream fs = new FileStream(Path.Combine(appDir, file), FileMode.Create, FileAccess.Write))
            {
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        byte[] fileBytes = webClient.DownloadData(OBJECTS_FOLDER + file);
                        fs.Write(fileBytes, 0, fileBytes.Length);
                    }
                    catch (Exception e)
                    {
                        throwError = e.Message;
                        return false;
                    }
                }
            }
            return true;
        }

    }
}
