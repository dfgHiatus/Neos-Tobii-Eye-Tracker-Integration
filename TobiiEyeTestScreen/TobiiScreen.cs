using SharpOSC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TobiiEyeTestScreen
{
    public class TobiiScreen
    {
        public Dictionary<string, float> eyeData = new Dictionary<string, float>()
        {
            { "/eye/left/x", 0 },
            { "/eye/left/y", 0 },
            { "/eye/left/z", 0 },
            { "/eye/right/x", 0 },
            { "/eye/right/y", 0 },
            { "eye/right/z", 0 },
            { "/gaze/x", 0 },
            { "/gaze/y", 0 },
            { "/fixation/x", 0 },
            { "/fixation/y", 0 },
            { "/timestamp" , 0}
        };

        public UDPListener listener;
        public OscBundle messageReceived;
        public static Thread thread;

        public void Start()
        {
            Process.Start(@"TobiiApp\gazeOSC.exe");
            Thread.Sleep(3000);

            // Create a thread to do the listening
            listener = new UDPListener(8888);
            thread = new Thread(new ThreadStart(ListenLoop));
            thread.Start();
        }

        public void Stop()
        {
            listener.Close();
            listener.Dispose();
            thread.Join();
        }

        private void ListenLoop()
        {
            while (true)
            {
                messageReceived = (OscBundle)listener.Receive();

                if (messageReceived != null)
                {
                    foreach (var msg in messageReceived.Messages) {

                        // Console.WriteLine("Number of messages: " + messageReceived.Messages.Count);
                        // Console.WriteLine(msg.Address.ToString());
                        // Console.WriteLine((float) msg.Arguments[0]);

                        // This should always contain the key. Unless...
                        if (eyeData.ContainsKey(msg.Address.ToString()))
                        {
                            eyeData[msg.Address.ToString()] = (float) msg.Arguments[0];
                            Console.WriteLine($"{msg.Address.ToString()}'s value is now {(float) msg.Arguments[0]}");
                        }
                    }
                    eyeData["/timestamp"] = messageReceived.Timetag;
                }
                Thread.Sleep(20);
            }
        }
    }
}