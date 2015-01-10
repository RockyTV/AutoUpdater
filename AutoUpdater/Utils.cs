using System;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.Collections.Generic;
using System.Net;

namespace AutoUpdater
{
    public class JenkinsAPI
    {
        public Dictionary<string, string> lastSuccessfulBuild { get; set; }
    }

    public static class Utils
    {
        private static int MAJOR_VERSION = 1;
        private static int MINOR_VERSION = 0;
        private static int BUILD_VERSION = 4;
        private static int REVISION = 0;

        public static string VERSION
        {
            get
            {
                return String.Format("{0}.{1}.{2}.{3}", MAJOR_VERSION, MINOR_VERSION, BUILD_VERSION, REVISION);
            }
        }

        public static string AUTHOR
        {
            get
            {
                return "RockyTV";
            }
        }

        #region ProcessExecutablePath
        public static string ProcessExecutablePath(Process process)
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
        #endregion
        #region IsRunningAsAdministrator
        public static bool IsRunningAsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }
        #endregion
    }
}
