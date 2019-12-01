using System;
using Microsoft.Lync.Model;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Management;
using System.Linq;
using System.Text.RegularExpressions;

namespace Status.Skype.Arduino.Nacho.Coll
{

    public class ArduinoThread
    {
        static object __serialLock = new object();
        static System.IO.Ports.SerialPort _serialPort = new System.IO.Ports.SerialPort(new System.ComponentModel.Container()) { WriteTimeout = 500, ReadTimeout = 500 };



        #region finish flag
        static object __finishLock = new object();
        static bool _finish = false;
        public static void Finish()
        {
            lock (__finishLock)
                _finish = true;
        }
        #endregion

        #region error flag
        static object __errorLock = new object();
        static bool _error = false;
        static bool Error
        {
            get { return _error; }
            set
            {
                lock (__errorLock)
                    _error = value;
            }
        }
        #endregion

        static Thread _thread = new Thread(SayHelloToArduino);
        public void Start()
        {
            Logger.Log("Starting ArduinoThread...");
            _thread.Start();
        }
        public static void Join()
        {
            Logger.Log("Stopping ArduinoThread...");
            _thread.Join();
            Logger.Log("ArduinoThread Stopped.");
        }

        static void SayHelloToArduino()
        {
            try
            {
                Logger.Log("ArduinoThread Started.");
                string skypeArduinoPort; bool needsUpdate = false;
                do
                {
                    try
                    {
                        if (needsUpdate || CatchSkypeArduinoPort(out skypeArduinoPort, out needsUpdate))
                            if (Error || needsUpdate) { UpdateSkypeArduinoLeds(LyncClientThread.Status); Error = false; }
                        Thread.Sleep(4000);
                    }
                    catch (Exception Ex)
                    {
                        needsUpdate = true;
                        Thread.Sleep(5000);
                        Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
                    }
                } while (!_finish);
                SayByeToArduino();
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }
        }

        static void SayByeToArduino()
        {
            try
            {
                Logger.Log("Bye Bye Arduino...");
                string skypeArduinoPort; bool needsUpdate;
                if (CatchSkypeArduinoPort(out skypeArduinoPort, out needsUpdate))
                    SendSerialMessageToSkypeArduino("?", skypeArduinoPort);
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }
        }

        public static void UpdateSkypeArduinoLeds(ContactAvailability currentAvailability)
        {

            try
            {
                string skypeArduinoPort; bool needsUpdate;
                if (CatchSkypeArduinoPort(out skypeArduinoPort, out needsUpdate))
                {
                    switch (currentAvailability)
                    {
                        case ContactAvailability.Free:
                        case ContactAvailability.FreeIdle:
                            SendSerialMessageToSkypeArduino("G", skypeArduinoPort);
                            break;
                        case ContactAvailability.Offline:
                        case ContactAvailability.Away:
                            SendSerialMessageToSkypeArduino("W", skypeArduinoPort);
                            break;
                        case ContactAvailability.Busy:
                        case ContactAvailability.TemporarilyAway:
                        case ContactAvailability.DoNotDisturb:
                            SendSerialMessageToSkypeArduino("R", skypeArduinoPort);
                            break;
                        default:
                            SendSerialMessageToSkypeArduino("?", skypeArduinoPort);
                            break;

                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }
        }
        private static void SendSerialMessageToSkypeArduino(string Message, string Port)
        {
            lock (__serialLock)
            {
                _serialPort.PortName = Port;
                try
                {

                    if (!_serialPort.IsOpen)
                        _serialPort.Open();
                    _serialPort.Write(Message);
                }
                catch (Exception Ex)
                {
                    Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
                    Error = true;
                }
                finally
                {
                    _serialPort.Close();
                }

            }
        }


        static bool CatchSkypeArduinoPort(out string Port, out bool NeedsUpdate)
        {
            Port = "COM1"; NeedsUpdate = false;
            string[] portNames = GetSerialPorts();
            foreach (string port in portNames)
            {
                lock (__serialLock)
                {

                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                    _serialPort.PortName = port;

                    if (!_serialPort.IsOpen)
                        _serialPort.Open();
                    try
                    {
                        _serialPort.Write("X");
                        int arduinoResponse = _serialPort.ReadChar();
                        if (arduinoResponse == 88 || arduinoResponse == 89) // Skype.arduino must return X or Y.
                        {
                            Port = port;
                            NeedsUpdate = arduinoResponse == 89;
                            return true;
                        }
                    }
                    catch
                    {
                        Error = true;
                    }
                    finally
                    {
                        _serialPort.Close();
                    }
                }
            }

            return false;
        }


        static string[] GetSerialPorts()
        {

            List<string> arduinoPorts = new List<string>();
            try
            {
                ManagementScope connectionScope = new ManagementScope();
                SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_PnPEntity WHERE (Name LIKE '%COM%')");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);
                Regex reg = new Regex(@"COM\d");

                foreach (ManagementObject item in searcher.Get())
                {

                    string name = item["Name"].ToString();
                    if (name.Contains("COM"))
                    {
                        try
                        {
                            string comPort = name.Substring(name.IndexOf("COM"), 4);
                            if (reg.IsMatch(comPort)) arduinoPorts.Add(comPort);
                        }
                        catch { }
                    }
                }

            }
            catch { }

            return arduinoPorts.Count > 0 ? arduinoPorts.ToArray() : SerialPort.GetPortNames();

        }
    }
}