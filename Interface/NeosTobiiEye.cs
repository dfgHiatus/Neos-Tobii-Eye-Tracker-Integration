using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using System.Reflection;
using static NeosTobiiEyeIntegration.TobiiProTrackingInterface;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		// TODO
		// Expose EyeTrackingOperations as *juicy* config vars
		// Possibly add calibration?

		public static TobiiProTrackingInterface tobiiProTrackingInterface = null;
		public static bool usingTobiiScreen = true;

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

		public static bool combinedIsValid;
		public static float combinedBlink;
		public static float3 combinedRawPos;
		public static float3 combinedRawRot;
		public static float combinedRawPupil;

		public static long timestamp;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-1.0.4";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		public override void OnEngineInit()
		{
			try 
			{
				// Harmony.DEBUG = true;
				tobiiProTrackingInterface = new TobiiProTrackingInterface();

				// Assuming there is only one Tobii device connected, this should suffice
				if (tobiiProTrackingInterface.Initialize())
				{
					tobiiProTrackingInterface.StartThread();
					// IIRC this doesn't work for VR Devices
					if (usingTobiiScreen)
                    {
						tobiiProTrackingInterface.eyeTracker.GazeDataReceived += Screen_OnGazeDataReceived;
					}
					else
                    {
						// Deprecated
						tobiiProTrackingInterface.eyeTracker.HMDGazeDataReceived += EyeTracker_HMDGazeDataReceived;
					}

					tobiiProTrackingInterface.eyeTracker.ConnectionLost += EyeTracker_ConnectionLost;
					tobiiProTrackingInterface.eyeTracker.ConnectionRestored += EyeTracker_ConnectionRestored;
					Debug(string.Format("Tobii eye tracker connected with the following stats: \n" +
										"Firmware Version {0}\n" +
										"Model {1}\n" +
										"Serial Number {2}\n" +
										"Device Name {3}\n" +
										"Operating Address {4}\n" +
										"Runtime Version {5}\n",
										tobiiProTrackingInterface.eyeTracker.FirmwareVersion,
										tobiiProTrackingInterface.eyeTracker.Model,
										tobiiProTrackingInterface.eyeTracker.SerialNumber,
										tobiiProTrackingInterface.eyeTracker.DeviceName,
										tobiiProTrackingInterface.eyeTracker.Address.ToString(),
										tobiiProTrackingInterface.eyeTracker.RuntimeVersion)
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



        [HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				if (tobiiProTrackingInterface.eyeTracker != null)
				{
					tobiiProTrackingInterface.Teardown();
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
			if (NeosTobiiEye.usingTobiiScreen)
			{
				eyes.IsEyeTrackingActive = !Engine.Current.InputInterface.VR_Active;
				eyes.LeftEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				// TODO Check this
				eyes.LeftEye.IsTracking = !Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsTracking = !Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsTracking = !Engine.Current.InputInterface.VR_Active;
			}
			else
			{
				eyes.IsEyeTrackingActive = Engine.Current.InputInterface.VR_Active;
				eyes.LeftEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
				// TODO Check this
				eyes.LeftEye.IsTracking = Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsTracking = Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsTracking = Engine.Current.InputInterface.VR_Active;
			}

			eyes.LeftEye.IsTracking = NeosTobiiEye.leftIsValid;
			eyes.RightEye.IsTracking = NeosTobiiEye.rightIsValid;
			eyes.CombinedEye.IsTracking = NeosTobiiEye.combinedIsValid;

			eyes.Timestamp = NeosTobiiEye.timestamp;

			eyes.LeftEye.Squeeze = 0f;
			eyes.RightEye.Squeeze = 0f;
			eyes.RightEye.Squeeze = 0f;

			eyes.LeftEye.Widen = 0f;
			eyes.RightEye.Widen = 0f;
			eyes.CombinedEye.Widen = 0f;

			eyes.LeftEye.Frown = 0f;
			eyes.RightEye.Frown = 0f;
			eyes.CombinedEye.Frown = 0f;

			eyes.LeftEye.Openness = NeosTobiiEye.leftBlink;
			eyes.RightEye.Openness = NeosTobiiEye.rightBlink;
			eyes.CombinedEye.Openness = NeosTobiiEye.combinedBlink;

			eyes.LeftEye.PupilDiameter = NeosTobiiEye.leftRawPupil;
			eyes.RightEye.PupilDiameter = NeosTobiiEye.rightRawPupil;
			eyes.CombinedEye.PupilDiameter = NeosTobiiEye.combinedRawPupil;

		}
	}
}