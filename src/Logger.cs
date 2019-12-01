
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Status.Skype.Arduino.Nacho.Coll
{
    public class Logger
    {
        const string _logFile = "log.txt";

        public class LEVEL { public const string DEBUG = "DEBUG"; public const string ERROR = "ERROR"; }
        protected static readonly object lockObj = new object();
        public static void Log(string Message, string Level = LEVEL.DEBUG, bool Append = true, [CallerMemberName] string CallerName = "")
        {
            lock (lockObj)
            {
                using (StreamWriter streamWriter = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _logFile), Append))
                {
                    streamWriter.WriteLine(DateTime.Now + "," + CallerName + "," + Level + "," + Message);
                    streamWriter.Close();
                }
            }
        }
    }
}