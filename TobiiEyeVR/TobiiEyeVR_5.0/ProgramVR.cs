using System;
using System.Threading;
using TobiiProTrackingInterface;

namespace TobiiEyeTestScreen
{
    class ProgramVR
    {
        static void Main(string[] args)
        {
            TobiiProVR tobiiProTrackingInterface = new TobiiProVR();
            tobiiProTrackingInterface.StartThread();
            Console.WriteLine(tobiiProTrackingInterface.Initialize(true, false).eyeSuccess ? "Found eye device" : "Could not find any eye devices");
            Console.WriteLine("Press enter to stop VR");
            Thread.Sleep(2000);

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
            {
                if (tobiiProTrackingInterface.gazeData != null)
                {
                    Console.WriteLine(tobiiProTrackingInterface.gazeData.LeftEye.GazeOrigin.PositionInHMDCoordinates.X);
                    Console.WriteLine(tobiiProTrackingInterface.gazeData.RightEye.GazeOrigin.PositionInHMDCoordinates.X);
                }
            }

            Console.WriteLine("Stopped");
            tobiiProTrackingInterface.Teardown();
        }
    }
}
