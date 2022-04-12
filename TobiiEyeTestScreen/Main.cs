using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using TobiiAPI;

namespace Tobii.StreamEngine.Sample
{
    public static class StreamSample
    {
        public static void Main()
        {
            /*            TobiiScreen tobiiScreen = new TobiiScreen();
                        tobiiScreen.IsDebug = true;

                        // Display device name and pause for 2 seconds
                        if (tobiiScreen.Init()) 
                        {
                            Thread.Sleep(10000);
                            tobiiScreen.Teardown();
                        }*/

            var connected = TobiiCompanionInterface.Connect();
            while (connected)
            {
                TobiiCompanionInterface.Update();
                Console.WriteLine(TobiiStruct.LeftIsDeviceTracking);
                Thread.Sleep(10);
            }
            TobiiCompanionInterface.Teardown();
        }

        public struct TobiiStruct
        {
            public static string deviceName;
            public static long Timestamp;
            public static bool IsDebug;

            // If the tracking should be used at all (here, Screen-mode vs VR)
            // These should always be true
            public static bool LeftIsDeviceActive;
            public static bool RightIsDeviceActive;
            public static bool CombinedIsDeviceActive;
            public static bool LeftIsTracking;
            public static bool RightIsTracking;
            public static bool CombinedIsTracking;

            // If the tracking is reliable enough to be used
            public static bool LeftIsDeviceTracking;
            public static bool RightIsDeviceTracking;
            public static bool CombinedIsDeviceTracking;

            public static Tuple<float, float, float> LeftEyeDirection;
            public static Tuple<float, float, float> RightEyeDirection;
            public static Tuple<float, float, float> CombinedEyeDirection;
            public static Tuple<float, float, float> LeftEyeRawPosition;
            public static Tuple<float, float, float> RightEyeRawPosition;
            public static Tuple<float, float, float> CombinedEyeRawPosition;

            // Normalized 0 to 1
            public static float LeftSqueeze;
            public static float RightSqueeze;
            public static float CombinedSqueeze;

            public static float LeftWiden;
            public static float RightWiden;
            public static float CombinedWiden;

            public static float LeftFrown;
            public static float RightFrown;
            public static float CombinedFrown;

            public static float LeftOpenness;
            public static float RightOpenness;
            public static float CombinedOpenness;

            // PupilDiameter is in mm
            public static float LeftPupilDiameter;
            public static float RightPupilDiameter;
            public static float CombinedPupilDiameter;

            public static IntPtr deviceContext;
            public static IntPtr apiContext;
            public static CancellationTokenSource _cancellationToken;
            public static tobii_error_t Error_T;
            public static Thread _worker;

        }

        public class TobiiCompanionInterface
        {
            public static MemoryMappedFile MemMapFile;
            public static MemoryMappedViewAccessor ViewAccessor;
            public static TobiiStruct memoryGazeData;
            public static Process CompanionProcess;
            public static bool hasMap;
            public static bool Connect()
            {
                CompanionProcess = new Process();
                CompanionProcess.StartInfo.FileName = "TobiiMemoryMap.exe";
                CompanionProcess.Start();

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        MemMapFile = MemoryMappedFile.OpenExisting("TobiiMemoryMap");
                        ViewAccessor = MemMapFile.CreateViewAccessor();
                        Console.WriteLine("Connected to Companion App!");
                        return true;
                    }
                    catch (FileNotFoundException)
                    {
                        Console.WriteLine("Trying to connect...");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not open the mapped file: " + ex);
                        return false;
                    }
                    Thread.Sleep(500);
                }

                return false;
            }

            public static void Update()
            {
                if (MemMapFile == null) return;
                ViewAccessor.Read(0, out memoryGazeData);
            }

            public static void Teardown()
            {
                if (MemMapFile == null) return;
                ViewAccessor.Write(0, ref memoryGazeData);
                MemMapFile.Dispose();
                CompanionProcess.Close();
            }
        }

    }
}