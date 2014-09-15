using System;
using System.Net;
using System.Text;
using DarkMultiPlayerServer;

namespace AutoUpdater
{
    public class AutoUpdater : DMPPlugin
    {
        public AutoUpdater()
        {
            DarkLog.Normal("[AutoUpdater] AutoUpdater v" + Common.VERSION + " initiated!");
        }

        public override void OnUpdate()
        {
            Common.DoCheck();
        }
    }
}
