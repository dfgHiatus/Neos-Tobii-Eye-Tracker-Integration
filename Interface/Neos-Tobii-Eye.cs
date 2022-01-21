using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using Tobii.Research;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Neos_Tobii_Eye_Integration
{
	public class Neos_Tobii_Eye : NeosMod
	{
		// TODO
		// Expose EyeTrackingOperations as *juicy* config vars
		// Possibly add calibration?

		public static IEyeTracker eyeTracker;

		public static bool leftIsValid;
		public static float leftBlink;
		public static float3 leftRawPos;
		public static float3 leftRawRot;
		public static float leftRawPupil;

		public static bool rightIsValid;
		public static float rightBlink;
		public static float3 rightRawPos;
		public static float3 rightRawRot;
		public static float rightRawPupil;

		public static long timestamp;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-1.0.2";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		public override void OnEngineInit()
		{
			try 
			{
				// Harmony.DEBUG = true;

				// Assuming there is only one Tobii device connected, this should suffice
				// Alt: var eyeTrackerList = EyeTrackingOperations.FindAllEyeTrackersAsync();
				var eyeTrackerList = EyeTrackingOperations.FindAllEyeTrackers(); 
				eyeTracker = eyeTrackerList.FirstOrDefault();

				if (eyeTracker != null)
				{
					// Attempt to initiallize the eye tracking MANAGER if the device supports it
					// This isn't needed to get eye tracking to work, but it is helpful
					// IIRC this doesn't work for VR Devices
					CallEyeTrackerManager(eyeTracker);
					eyeTracker.HMDGazeDataReceived += EyeTracker_HMDGazeDataReceived;
					eyeTracker.ConnectionLost += EyeTracker_ConnectionLost;
					eyeTracker.ConnectionRestored += EyeTracker_ConnectionRestored;
					Debug(String.Format("Tobii eye tracker connected with the following stats: \n" +
										"Firmware Version {0}\n" +
										"Model {1}\n" +
										"Serial Number {2}\n" +
										"DeviceName {3}\n" +
										"Operating Address {4}\n" +
										"RuntimeVersion {5}\n",
										eyeTracker.FirmwareVersion,
										eyeTracker.Model,
										eyeTracker.SerialNumber,
										eyeTracker.DeviceName,
										eyeTracker.Address.ToString(),
										eyeTracker.RuntimeVersion)
						);
				}
				else
				{
					Warn("No Tobii eye tracker was found. Tobii eye tracking will be unavalible for this session.");
				}
            }
			catch (ReflectionTypeLoadException)
			{
				Error("Couldn't find the Tobii DLLs in the Neos base directory. Please check that Tobii.Research.dll, tobii_pro.dll, and tobii_firmware_upgrade.dll are present.");
			}
			catch (Exception e)
            {
				Error("An unexpected error occured when trying to initiallie Tobii Eye Tracking.");
				Error(e.Message);
            }

			Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
			harmony.PatchAll();
		}

		public static void EyeTracker_ConnectionLost(object sender, ConnectionLostEventArgs e)
        {
			Debug("Connection was lost to the Tobii eye tracking service.");
        }

		public static void EyeTracker_ConnectionRestored(object sender, ConnectionRestoredEventArgs e)
        {
			Debug("Connection was restored to the Tobii eye tracking service.");
        }

		private static void EyeTracker_HMDGazeDataReceived(object sender, HMDGazeDataEventArgs e)
		{
			// TODO Check if both are valid post debugging

			leftIsValid = e.LeftEye.GazeOrigin.Validity == Validity.Valid;
			leftBlink = e.LeftEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			leftRawPupil = e.LeftEye.Pupil.PupilDiameter;

			if (e.LeftEye.GazeOrigin.Validity == Validity.Valid && e.LeftEye.GazeDirection.Validity == Validity.Valid)
            {
				leftRawPos = new float3(
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.X,
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.Y,
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.Z);
				leftRawRot = new float3(
					e.LeftEye.GazeDirection.UnitVector.X,
					e.LeftEye.GazeDirection.UnitVector.Y,
					e.LeftEye.GazeDirection.UnitVector.Z);
			}

			rightIsValid = e.RightEye.GazeOrigin.Validity == Validity.Valid;
			rightBlink = e.RightEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			rightRawPupil = e.RightEye.Pupil.PupilDiameter;

			if (e.RightEye.GazeOrigin.Validity == Validity.Valid && e.RightEye.GazeDirection.Validity == Validity.Valid)
			{
				rightRawPos = new float3(
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.X,
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.Y,
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.Z);
				rightRawRot = new float3(
					e.RightEye.GazeDirection.UnitVector.X,
					e.RightEye.GazeDirection.UnitVector.Y,
					e.RightEye.GazeDirection.UnitVector.Z);
			}

			timestamp = e.DeviceTimeStamp;

		}

		private static void CallEyeTrackerManager(IEyeTracker eyeTracker)
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
					Debug("Eye Tracker Manager was called successfully!");
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
				Warn(e.Message);
			}
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				if (eyeTracker != null)
				{
					eyeTracker.HMDGazeDataReceived -= EyeTracker_HMDGazeDataReceived;
					eyeTracker.ConnectionLost -= EyeTracker_ConnectionLost;
					eyeTracker.ConnectionRestored -= EyeTracker_ConnectionRestored;
					EyeTrackingOperations.Terminate();
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
		[HarmonyPatch(new[] { typeof(Engine) })]
		public class InputInterfaceCtorPatch
		{
			public static void Postfix(InputInterface __instance)
			{
				try
				{
					GenericInputDevice gen = new GenericInputDevice();
					Debug("Module Name: " + gen.ToString());
					__instance.RegisterInputDriver(gen);
				}
				catch (Exception e)
				{
					Warn("Module failed to initiallize.");
					Warn(e.Message);
				}
			}
		}
	}

	class GenericInputDevice : IInputDriver
	{
		public Eyes eyes;
		public int UpdateOrder => 100;

		public void CollectDeviceInfos(BaseX.DataTreeList list)
		{
			DataTreeDictionary EyeDataTreeDictionary = new DataTreeDictionary();
			EyeDataTreeDictionary.Add("Name", "Tobii Eye Tracking");
			EyeDataTreeDictionary.Add("Type", "Eye Tracking");
			EyeDataTreeDictionary.Add("Model", "Vive Eye Devkit");
			list.Add(EyeDataTreeDictionary);
		}

		public void RegisterInputs(InputInterface inputInterface)
		{
			eyes = new Eyes(inputInterface, "Tobii Eye Tracking");
		}

		public void UpdateInputs(float deltaTime)
		{
			eyes.IsEyeTrackingActive = Engine.Current.InputInterface.VR_Active;
			eyes.LeftEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
			eyes.RightEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
			eyes.CombinedEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;

			eyes.LeftEye.IsTracking = Neos_Tobii_Eye.leftIsValid;
			eyes.RightEye.IsTracking = Neos_Tobii_Eye.rightIsValid;
			eyes.CombinedEye.IsTracking = Neos_Tobii_Eye.leftIsValid || Neos_Tobii_Eye.rightIsValid;

			eyes.Timestamp = Neos_Tobii_Eye.timestamp;

			eyes.LeftEye.Squeeze = 0f;
			eyes.RightEye.Squeeze = 0f;
			eyes.RightEye.Squeeze = 0f;

			eyes.LeftEye.Widen = 0f;
			eyes.RightEye.Widen = 0f;
			eyes.CombinedEye.Widen = 0f;

			eyes.LeftEye.Frown = 0f;
			eyes.RightEye.Frown = 0f;
			eyes.CombinedEye.Frown = 0f;

			eyes.LeftEye.Openness = Neos_Tobii_Eye.leftBlink;
			eyes.RightEye.Openness = Neos_Tobii_Eye.rightBlink;
			eyes.CombinedEye.Openness = MathX.Clamp01(Neos_Tobii_Eye.leftBlink + Neos_Tobii_Eye.rightBlink);

			eyes.LeftEye.PupilDiameter = Neos_Tobii_Eye.leftRawPupil;
			eyes.RightEye.PupilDiameter = Neos_Tobii_Eye.rightRawPupil;
			eyes.CombinedEye.PupilDiameter = MathX.Average(Neos_Tobii_Eye.leftRawPupil + Neos_Tobii_Eye.rightRawPupil);

		}
	}
}