using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Tobii.Research;
using NeosModLoader;
using BaseX;
using static Neos_Tobii_Eye_Integration.Neos_Tobii_Eye;

namespace Neos_Tobii_Eye_Integration.Helpers
{
    public class TobiiScreen : NeosMod
    {
        // Problem?
        public override string Name => "Neos-Tobii-Eye-Integration";
        public override string Author => "dfgHiatus";
        public override string Version => "alpha-1.0.3";

        public static void CallEyeTrackerManager(IEyeTracker eyeTracker)
        {
            string etmStartupMode = "displayarea";
            string etmBasePath = Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"),
                                                                "TobiiProEyeTrackerManager"));
            string appFolder = Directory.EnumerateDirectories(etmBasePath, "app*").FirstOrDefault();
            string executablePath = Path.GetFullPath(Path.Combine(etmBasePath,
                                                                    appFolder,
                                                                    "TobiiProEyeTrackerManager.exe"));
            string arguments = "--device-address=" + eyeTracker.Address + " --mode=" + etmStartupMode;
            try
            {
                Process etmProcess = new Process();
                // Redirect the output stream of the child process.
                etmProcess.StartInfo.UseShellExecute = false;
                etmProcess.StartInfo.RedirectStandardError = true;
                etmProcess.StartInfo.RedirectStandardOutput = true;
                etmProcess.StartInfo.FileName = executablePath;
                etmProcess.StartInfo.Arguments = arguments;
                etmProcess.Start();
                string stdOutput = etmProcess.StandardOutput.ReadToEnd();

                etmProcess.WaitForExit();
                int exitCode = etmProcess.ExitCode;
                if (exitCode == 0)
                {
                    Debug("Eye Tracker Manager was called successfully");
                }
                else
                {
                    Warn("Eye Tracker Manager call returned the error code: {0}", exitCode);
                    foreach (string line in stdOutput.Split(Environment.NewLine.ToCharArray()))
                    {
                        if (line.StartsWith("ETM Error:"))
                        {
                            Error(line);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("An unexpected error occured when trying to initiallize the Tobii Eye Manager.");
                Error(e.Message);
            }
        }

        public static void EyeTracker_GazeDataReceived(object sender, GazeDataEventArgs e)
        {
            // TODO Check if both are valid post debugging

            leftIsValid = e.LeftEye.GazeOrigin.Validity == Validity.Valid;
            leftBlink = e.LeftEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
            leftRawPupil = e.LeftEye.Pupil.PupilDiameter;

            if (e.LeftEye.GazeOrigin.Validity == Validity.Valid && e.LeftEye.GazePoint.Validity == Validity.Valid)
            {
                leftRawPos = new float3(
                    e.LeftEye.GazeOrigin.PositionInUserCoordinates.X,
                    e.LeftEye.GazeOrigin.PositionInUserCoordinates.Y,
                    e.LeftEye.GazeOrigin.PositionInUserCoordinates.Z);
                leftRawRot = new float3(
                    e.LeftEye.GazePoint.PositionInUserCoordinates.X,
                    e.LeftEye.GazePoint.PositionInUserCoordinates.Y,
                    e.LeftEye.GazePoint.PositionInUserCoordinates.Z);
            }

            rightIsValid = e.RightEye.GazeOrigin.Validity == Validity.Valid;
            rightBlink = e.RightEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
            rightRawPupil = e.RightEye.Pupil.PupilDiameter;

            if (e.RightEye.GazeOrigin.Validity == Validity.Valid && e.RightEye.GazePoint.Validity == Validity.Valid)
            {
                rightRawPos = new float3(
                    e.RightEye.GazeOrigin.PositionInUserCoordinates.X,
                    e.RightEye.GazeOrigin.PositionInUserCoordinates.Y,
                    e.RightEye.GazeOrigin.PositionInUserCoordinates.Z);
                rightRawRot = new float3(
                    e.RightEye.GazePoint.PositionInUserCoordinates.X,
                    e.RightEye.GazePoint.PositionInUserCoordinates.Y,
                    e.RightEye.GazePoint.PositionInUserCoordinates.Z);
            }

            timestamp = e.DeviceTimeStamp;

        }
    }
}
