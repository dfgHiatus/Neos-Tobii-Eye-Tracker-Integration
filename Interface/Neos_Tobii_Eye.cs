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
using Neos_Tobii_Eye_Integration.Helpers;

namespace Neos_Tobii_Eye_Integration
{
	public class Neos_Tobii_Eye : NeosMod
	{
		// TODO
		// Expose EyeTrackingOperations as *juicy* config vars
		// Possibly add calibration?

		public static IEyeTracker eyeTracker;
		public static bool usingTobiiScreen = false;

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
		public override string Version => "alpha-1.0.3";
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
					if (usingTobiiScreen)
                    {
						eyeTracker.GazeDataReceived += TobiiScreen.EyeTracker_GazeDataReceived;
						TobiiScreen.CallEyeTrackerManager(eyeTracker);
					}
					else
                    {
						eyeTracker.HMDGazeDataReceived += TobiiVR.EyeTracker_HMDGazeDataReceived;
					}
					
					eyeTracker.ConnectionLost += EyeTracker_ConnectionLost;
					eyeTracker.ConnectionRestored += EyeTracker_ConnectionRestored;
					Debug(string.Format("Tobii eye tracker connected with the following stats: \n" +
										"Firmware Version {0}\n" +
										"Model {1}\n" +
										"Serial Number {2}\n" +
										"Device Name {3}\n" +
										"Operating Address {4}\n" +
										"Runtime Version {5}\n",
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
					Warn("No Tobii eye tracker was found. Tobii eye tracking will be unavailble for this session.");
				}
            }
			catch (ReflectionTypeLoadException)
			{
				Error("Couldn't find the Tobii DLLs in the Neos base directory. Please check that Tobii.Research.dll, tobii_pro.dll, and tobii_firmware_upgrade.dll are present.");
			}
			catch (Exception e)
            {
				Error("An unexpected error occured when trying to initiallize Tobii Eye Tracking.");
				Error(e.Message);
            }
			finally
            {
				Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
				harmony.PatchAll();
			}
		}

		public static void EyeTracker_ConnectionLost(object sender, ConnectionLostEventArgs e)
        {
			Debug("Connection was lost to the Tobii eye tracking service.");
        }

		public static void EyeTracker_ConnectionRestored(object sender, ConnectionRestoredEventArgs e)
        {
			Debug("Connection was restored to the Tobii eye tracking service.");
        }

        [HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				if (eyeTracker != null)
				{
					if (usingTobiiScreen)
                    {
						eyeTracker.GazeDataReceived -= TobiiScreen.EyeTracker_GazeDataReceived;
					}
					else
                    {
						eyeTracker.HMDGazeDataReceived -= TobiiVR.EyeTracker_HMDGazeDataReceived;
					}
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
			if (Neos_Tobii_Eye.usingTobiiScreen)
			{
				eyes.IsEyeTrackingActive = !Engine.Current.InputInterface.VR_Active;
				eyes.LeftEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
			}
			else
			{
				eyes.IsEyeTrackingActive = Engine.Current.InputInterface.VR_Active && Neos_Tobii_Eye.usingTobiiScreen;
				eyes.LeftEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
			}

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