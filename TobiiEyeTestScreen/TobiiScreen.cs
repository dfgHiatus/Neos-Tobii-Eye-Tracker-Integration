using SharpOSC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TobiiEyeTestScreen
{
    public class TobiiScreen
    {
        public float[] eyeData = new float[11];

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

                try
                {
                    if (messageReceived != null)
                    {
                        for (int i = 0; i < 11; i++)
                        {
                            // Console.WriteLine("Number of messages: " + messageReceived.Messages.Count);
                            // Console.WriteLine(msg.Address.ToString());
                            // Console.WriteLine((float) msg.Arguments[0]);
                            // Console.WriteLine($"{msg.Address.ToString()}'s value is now {(float)msg.Arguments[0]}");

                            eyeData[i] = (float)messageReceived.Messages[i].Arguments[0];
                        }
                        eyeData[eyeData.Length - 1] = messageReceived.Timetag;

                    }
                }
                catch (Exception e) { continue; }
                Thread.Sleep(20);
            }
        }
    }
}