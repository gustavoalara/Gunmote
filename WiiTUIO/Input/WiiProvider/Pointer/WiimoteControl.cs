using HidLibrary;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WiimoteLib;
using WiiTUIO.ArcadeHook;
using WiiTUIO.Output.Handlers;
using WiiTUIO.Properties;
using WiiTUIO.DeviceUtils;
using WindowsInput;

namespace WiiTUIO.Provider
{
    class WiimoteControl
    {
        public DateTime LastWiimoteEventTime = DateTime.Now; //Last time recieved an update
        public DateTime LastSignificantWiimoteEventTime = DateTime.Now; //Last time when updated the cursor or button config. Used for power saving features.

        public Wiimote Wiimote;
        public WiimoteStatus Status;

        /// <summary>
        /// Used to obtain mutual exlusion over Wiimote updates.
        /// </summary>
        public Mutex WiimoteMutex = new Mutex();
        private WiiKeyMapper keyMapper;
        private OutputProvider arcadeHook;
        private bool firstConfig = true;
        private string currentKeymap;
        private HandlerFactory handlerFactory;

        public WiimoteControl(int id, Wiimote wiimote)
        {
            this.Wiimote = wiimote;
            this.Status = new WiimoteStatus();
            this.Status.ID = id;

            this.handlerFactory = new HandlerFactory();

            HidDevice hidDevice = HidDevices.GetDevice(this.Wiimote.HIDDevicePath);
            hidDevice.ReadSerialNumber(out byte[] data);
            //string serialNumber = Settings.Default.pointer_4IRMode != "none" ? System.Text.Encoding.Unicode.GetString(data, 0, 24) : null;
			string serialNumber = System.Text.Encoding.Unicode.GetString(data, 0, 24);
            this.keyMapper = new WiiKeyMapper(wiimote, id, handlerFactory, serialNumber);
            this.arcadeHook = new OutputProvider(id);

            this.keyMapper.OnButtonDown += WiiButton_Down;
            this.keyMapper.OnButtonUp += WiiButton_Up;
            this.keyMapper.OnConfigChanged += WiiKeyMap_ConfigChanged;
            this.keyMapper.OnRumble += WiiKeyMap_OnRumble;
            this.keyMapper.OnLED += WiiKeyMap_OnLED;
            this.keyMapper.OnSpeaker += WiiKeyMap_OnSpeaker;
            this.arcadeHook.OnOutput += ArcadeHook_OnOutput;
        }

        private void WiiKeyMap_OnRumble(bool rumble)
        {
            Console.WriteLine("Set rumble to: "+rumble);
            WiimoteMutex.WaitOne();
            this.Wiimote.SetRumble(rumble);
            WiimoteMutex.ReleaseMutex();
        }

        private void WiiKeyMap_OnLED(int index, bool on)
        {
            WiimoteMutex.WaitOne();
            switch (index)
            {
                case 1:
                    this.Wiimote.SetLEDs(on, this.Wiimote.WiimoteState.LEDState.LED2, this.Wiimote.WiimoteState.LEDState.LED3, this.Wiimote.WiimoteState.LEDState.LED4);
                    break;
                case 2:
                    this.Wiimote.SetLEDs(this.Wiimote.WiimoteState.LEDState.LED1, on, this.Wiimote.WiimoteState.LEDState.LED3, this.Wiimote.WiimoteState.LEDState.LED4);
                    break;
                case 3:
                    this.Wiimote.SetLEDs(this.Wiimote.WiimoteState.LEDState.LED1, this.Wiimote.WiimoteState.LEDState.LED2, on, this.Wiimote.WiimoteState.LEDState.LED4);
                    break;
                case 4:
                    this.Wiimote.SetLEDs(this.Wiimote.WiimoteState.LEDState.LED1, this.Wiimote.WiimoteState.LEDState.LED2, this.Wiimote.WiimoteState.LEDState.LED3, on);
                    break;
                default:
                    this.Wiimote.SetLEDs(this.Status.ID == 1, this.Status.ID == 2, this.Status.ID == 3, this.Status.ID == 4);
                    break;
            }
            WiimoteMutex.ReleaseMutex();
        }

        private void WiiKeyMap_OnSpeaker(string filename)
        {
            if (filename == null)
            {
                this.Wiimote.StopPlayback();
                return;
            }

            int maxPlaybackTime = 3500; // Max playback time in milliseconds

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename + ".wav");

            if (AudioUtil.IsValid(filename)) // Check for valid file or convert file if necessary
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        reader.BaseStream.Seek(44, SeekOrigin.Begin);   // Skip WAV header

                        byte[] soundData = reader.ReadBytes((int)(fs.Length - 44));

                        int maxBytes = (int)(this.Wiimote.WiimoteState.SpeakerState.SampleRate * (maxPlaybackTime / 1000.0) * 0.5);
                        if (soundData.Length > maxBytes)
                            Array.Resize(ref soundData, maxBytes);  // Truncate to max playback time

                        this.Wiimote.StartPlayback(soundData);
                    }
                }
            }
        }

        private void ArcadeHook_OnOutput(string key, string value)
        {
            int val = int.Parse(value);
            try
            {
                switch (key)
                {
                    case "Rumble":
                        WiiKeyMap_OnRumble(val > 0);
                        break;
                    case "LED":
                        WiimoteMutex.WaitOne();
                        this.Wiimote.SetLEDs(val == 1, val == 2, val == 3, val == 4);
                        WiimoteMutex.ReleaseMutex();
                        break;
                    case "LEDFill":
                        WiimoteMutex.WaitOne();
                        this.Wiimote.SetLEDs(val >= 1, val >= 2, val >= 3, val >= 4);
                        WiimoteMutex.ReleaseMutex();
                        break;
                    case "Sound":
                        WiiKeyMap_OnSpeaker(value);
                        break;
                    case "MameStop":
                        WiiKeyMap_OnRumble(false);
                        WiimoteMutex.WaitOne();
                        this.Wiimote.SetLEDs(this.Status.ID == 1, this.Status.ID == 2, this.Status.ID == 3, this.Status.ID == 4);
                        WiimoteMutex.ReleaseMutex();
                        break;
                }
            }
            catch { }
        }


        private void WiiKeyMap_ConfigChanged(WiiKeyMapConfigChangedEvent evt)
        {
            if (firstConfig)
            {
                currentKeymap = evt.Filename;
                firstConfig = false;
            }
            else if(evt.Filename != currentKeymap)
            {
                currentKeymap = evt.Filename;
                if (Settings.Default.notifications_enabled)
                    OverlayWindow.Current.ShowNotice("Perfil para el Wiimote " + this.Status.ID + " cambiado a \"" + evt.Name + "\"", this.Status.ID);
            }
        }

        private void WiiButton_Up(WiiButtonEvent evt)
        {
        }

        private void WiiButton_Down(WiiButtonEvent evt)
        {
        }
        
        public bool handleWiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Obtain mutual excluseion.
            WiimoteMutex.WaitOne();

            bool significant = false;

            try
            {
                WiimoteState ws = e.WiimoteState;
                this.Status.Battery = (ws.Battery > 0xc8 ? 0xc8 : (int)ws.Battery);

                significant = keyMapper.processWiimoteState(ws);

                if (significant)
                {
                    this.LastSignificantWiimoteEventTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling Wiimote in WiimoteControl: " + ex.Message);
                return significant;
            }
            //this.BatteryState = (pState.Battery > 0xc8 ? 0xc8 : (int)pState.Battery);
            
            // Release mutual exclusion.
            WiimoteMutex.ReleaseMutex();
            return significant;
        }

        public void Teardown()
        {
            this.keyMapper.Teardown();
        }
    }
}
