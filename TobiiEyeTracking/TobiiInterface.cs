using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using FrooxEngine;
using BaseX;

namespace NeosTobiiEyeIntegration
{
    public struct Vector
    {
        public Validity validity;
        public double x;
        public double y;
        public double z;
    }
    public enum Validity
    {
        Invalid = 0,
        Valid = 1
    }

    public struct DeviceInfo
    {
        public string name;
    }
    public struct GazeData
    {
        public DeviceInfo deviceInfo;
        public bool isActive;
        public Eye leftEye;                 //!< Left eye gaze ray.
        public Eye rightEye;                //!< Right eye gaze ray.
        public long timestamp;
    }

    public struct Eye
    {
        public Vector origin;
        public Vector direction;
    }

    public class TobiiCompanionInterface
    {
        public static MemoryMappedFile MemMapFile;
        public static MemoryMappedViewAccessor ViewAccessor;
        public static GazeData gazeData;
        public static Process CompanionProcess;

        public static bool Connect()
        {
            var modDir = Path.Combine(Engine.Current.AppPath, "nml_mods");

            CompanionProcess = new Process();
            CompanionProcess.StartInfo.WorkingDirectory = modDir;
            CompanionProcess.StartInfo.FileName = Path.Combine(modDir, "TobiiEyeTracking.exe");
            CompanionProcess.Start();

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    MemMapFile = MemoryMappedFile.OpenExisting("TobiiEyeTracking");
                    ViewAccessor = MemMapFile.CreateViewAccessor();
                    UniLog.Log("Connected to Companion App!");
                    return true;
                }
                catch (FileNotFoundException)
                {
                    UniLog.Log($"Trying to connect to the Companion App. Attempt {i}/5");
                }
                catch (Exception ex)
                {
                    UniLog.Log("Could not open the mapped file: " + ex);
                    return false;
                }
                Thread.Sleep(500);
            }

            return false;
        }

        public void Update()
        {
            if (MemMapFile == null) 
                return;
            ViewAccessor.Read(0, out gazeData);
        }

        public void Teardown()
        {
            if (MemMapFile == null) return;
            // memoryGazeData.shutdown = true; // tell the companion app to shut down gracefully but it doesn't work anyway
            ViewAccessor.Write(0, ref gazeData);
            MemMapFile.Dispose();
            CompanionProcess.Close();
        }
    }
}