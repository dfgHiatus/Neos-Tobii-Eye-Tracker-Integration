using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using TobiiEyeScreen;
using TobiiEyeVR;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		public static bool isScreen = true; // Make config
		public static TobiiScreen tobiiScreen;

		public static bool initialized = false;
		public static bool running = true;
		public static Native.Tobii_api_t tobiiAPI;
		public static Native.Tobii_device_t tobiiDevice;
		public static Native.Tobii_wearable_consumer_data_t eyeDataVR;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-1.0.6";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		public override void OnEngineInit()
		{
			try 
			{
				// Harmony.DEBUG = true;
				if (isScreen)
				{
					tobiiScreen = new TobiiScreen();
					tobiiScreen.Start();
				}
				else
				{
					Native.Tobii_api_create(out tobiiAPI, IntPtr.Zero, IntPtr.Zero);
					Native.Tobii_enumerate_local_device_urls(tobiiAPI, setupVRDevice, IntPtr.Zero);
				}		
            }
			catch (Exception e)
            {
				Warn("Tobii eye tracking will be unavailble for this session.");
				Error(e.Message);
            }
			finally
            {
				Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
				harmony.PatchAll();
			}
		}

		private static void setupVRDevice(string device, IntPtr intptr)
		{
			try
			{
				// In the event there are many devices, choose 1
				Debug($"Devices Detected: {device}");
				if (initialized)
					return;
				initialized = true;
				Debug($"Establishing connection to {device}...");
				Native.Tobii_device_create(tobiiAPI, device, Native.Tobii_field_of_use_t.FieldOfUseInteractive, out tobiiDevice);
				Native.Tobii_wearable_consumer_data_subscribe(tobiiDevice, getData, IntPtr.Zero);
				Debug($"Connected.");
			}
			catch (Exception e)
			{
				Error(e.Message);
			}
		}

		private unsafe static void getData(in Native.Tobii_wearable_consumer_data_t data, IntPtr user_data)
		{
			try
			{
				eyeDataVR = data;
			}
			catch (Exception e)
			{
				Error(e.Message);
			}
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				if (isScreen)
				{
					tobiiScreen.Stop();
				}
				else
				{
					Native.Tobii_wearable_consumer_data_unsubscribe(tobiiDevice);
					Native.Tobii_device_destroy(tobiiDevice);
					Native.Tobii_api_destroy(tobiiAPI);
					running = false;
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

		public class GenericInputDevice : IInputDriver
		{
			public Eyes eyes;
			public int UpdateOrder => 100;

			public void CollectDeviceInfos(BaseX.DataTreeList list)
			{
				DataTreeDictionary EyeDataTreeDictionary = new DataTreeDictionary();
				EyeDataTreeDictionary.Add("Name", "Tobii Eye Tracking");
				EyeDataTreeDictionary.Add("Type", "Eye Tracking");
				EyeDataTreeDictionary.Add("Model", "Unknown (Vive Eye Devkit/4C/5?)");
				list.Add(EyeDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, "Tobii Eye Tracking");
			}

			public unsafe void UpdateInputs(float deltaTime)
			{
				if (isScreen)
				{
					eyes.LeftEye.IsTracking = !Engine.Current.InputInterface.VR_Active;
					eyes.RightEye.IsTracking = !Engine.Current.InputInterface.VR_Active;

					eyes.Timestamp = tobiiScreen.eyeData["/timestamp"];

					eyes.LeftEye.RawPosition = new float3(
						tobiiScreen.eyeData["/eye/left/x"],
						tobiiScreen.eyeData["/eye/left/y"],
						tobiiScreen.eyeData["/eye/left/z"]);

					eyes.RightEye.RawPosition = new float3(
						tobiiScreen.eyeData["/eye/right/x"],
						tobiiScreen.eyeData["/eye/right/y"],
						tobiiScreen.eyeData["/eye/right/z"]);

					eyes.CombinedEye.RawPosition = new float3(
						MathX.Average(tobiiScreen.eyeData["/eye/left/x"], tobiiScreen.eyeData["/eye/right/x"]),
						MathX.Average(tobiiScreen.eyeData["/eye/left/y"], tobiiScreen.eyeData["/eye/right/y"]),
						MathX.Average(tobiiScreen.eyeData["/eye/left/z"], tobiiScreen.eyeData["/eye/right/z"]));
				}
				else
				{
					eyes.LeftEye.IsTracking = Engine.Current.InputInterface.VR_Active;
					eyes.RightEye.IsTracking = Engine.Current.InputInterface.VR_Active;
					eyes.CombinedEye.IsTracking = Engine.Current.InputInterface.VR_Active;

					eyes.LeftEye.Openness = (int) eyeDataVR.left.blink == 1 ? 1f : 0f;
					eyes.RightEye.Openness = (int) eyeDataVR.right.blink == 1 ? 1f : 0f;
					eyes.CombinedEye.Openness = (int) eyeDataVR.left.blink == 1 || (int)eyeDataVR.right.blink == 1 ? 1f : 0f;

					eyes.CombinedEye.Direction = new float3(
						eyeDataVR.gaze_direction_combined_normalized_xyz[0],
						eyeDataVR.gaze_direction_combined_normalized_xyz[1],
						eyeDataVR.gaze_direction_combined_normalized_xyz[2]);

					eyes.ConvergenceDistance = (int) eyeDataVR.convergence_distance_validity == 1 ? eyeDataVR.convergence_distance_mm : 0f;
					eyes.Timestamp = eyeDataVR.timestamp_us;
				}
			}
		}
    }
}