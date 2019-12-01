using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace Status.Skype.Arduino.Nacho.Coll
{

    public partial class App : Application
    {

        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            /* Credits on NotifyIcon features: https://github.com/hardcodet/wpf-notifyicon */
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            try
            {
                Logger.Log("Application Start.", Logger.LEVEL.DEBUG, false);
                new LyncClientThread(ArduinoThread.UpdateSkypeArduinoLeds).Start();
                new ArduinoThread().Start();
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR, false);
            }

        }

        protected override void OnExit(ExitEventArgs e)
        {

            LyncClientThread.Finish(); ArduinoThread.Finish();
            LyncClientThread.Join(); ArduinoThread.Join();
            Logger.Log("Application Stopped.");

            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }
    }
}
