using System;
using Microsoft.Lync.Model;
using System.IO.Ports;
using System.Threading;


namespace Status.Skype.Arduino.Nacho.Coll
{

    public class LyncClientThread
    {

        static LyncClient _lyncClient;
        static object __lyncClientLock = new object();

        #region status
        static ContactAvailability _status = ContactAvailability.None;
        static object __statusLock = new object();
        public static ContactAvailability Status
        {
            get { return _status; }
            private set
            {
                lock (__statusLock)
                {
                    _status = value;
                }
                if (_skypeStatusChanged != null) _skypeStatusChanged(_status);
            }
        }
        public delegate void SkypeStatusChanged(ContactAvailability Status);
        static SkypeStatusChanged _skypeStatusChanged;
        #endregion

        #region finish flag
        static bool _finish = false;
        static object __finishLock = new object();

        public static void Finish()
        {
            lock (__finishLock)
                _finish = true;
        }
        #endregion

        static Thread _thread;
        public LyncClientThread(SkypeStatusChanged SkypeStatusChanged)
        {
            _skypeStatusChanged = SkypeStatusChanged;
            _thread = new Thread(CheckLyncClient);
        }

        public void Start()
        {
            Logger.Log("Starting LyncClientThread...");
            _thread.Start();
        }
        public static void Join()
        {
            Logger.Log("Stopping LyncClientThread...");
            _thread.Join();
            Logger.Log("LyncClientThread Stopped.");
        }

        static void CheckLyncClient()
        {
            try
            {
                Logger.Log("LyncClientThread Started.");
                do
                {
                    lock (__lyncClientLock)
                    {
                        if (_lyncClient == null)
                        {
                            try
                            {
                                _lyncClient = LyncClient.GetClient();
                                _lyncClient.StateChanged += new EventHandler<ClientStateChangedEventArgs>(SkypeClientStateChenged);
                                _lyncClient.Self.Contact.ContactInformationChanged += new EventHandler<ContactInformationChangedEventArgs>(SkypeSelfStateChenged);
                                UpdateSkypeAvilability();
                            }
                            catch { _lyncClient = null; }
                        }

                    }
                    Thread.Sleep(3000);
                } while (!_finish);
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }


        }

        private static void SkypeClientStateChenged(object sender, ClientStateChangedEventArgs e)
        {
            try
            {
                switch (e.NewState)
                {
                    case Microsoft.Lync.Model.ClientState.ShuttingDown:
                    case Microsoft.Lync.Model.ClientState.SignedOut:
                    case Microsoft.Lync.Model.ClientState.SigningOut:
                    case Microsoft.Lync.Model.ClientState.Invalid:
                        _lyncClient = null; Status = ContactAvailability.None;
                        break;
                }
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }

        }

        static void SkypeSelfStateChenged(object sender, ContactInformationChangedEventArgs e)
        {
            try
            {
                foreach (ContactInformationType contactInfoType in e.ChangedContactInformation)
                {
                    switch (contactInfoType)
                    {
                        case ContactInformationType.Availability:
                            UpdateSkypeAvilability();
                            break;
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }
        }

        private static void UpdateSkypeAvilability()
        {
            try
            {
                if ((_lyncClient?.State == ClientState.SignedIn))
                    Status = ((ContactAvailability)_lyncClient.Self.Contact.GetContactInformation(ContactInformationType.Availability));
            }
            catch (Exception Ex)
            {
                Logger.Log(Ex.Message, Logger.LEVEL.ERROR);
            }
        }

    }
}