using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using TobiiEyeBridge;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		// Tobii
		public static bool initialized = false;
		public static bool running = true;
		public static Native.Tobii_api_t tobiiAPI;
		public static Native.Tobii_device_t tobiiDevice;
		public static Native.Tobii_device_info_t tobiiDeviceInfo;

		// VR
		public static Native.Tobii_wearable_consumer_data_t eyeDataVR;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-vr-1.0.7";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		public override void OnEngineInit()
		{
			try
			{
				Native.Tobii_api_create(out tobiiAPI, IntPtr.Zero, IntPtr.Zero);
				Native.Tobii_enumerate_local_device_urls(tobiiAPI, setupDevice, IntPtr.Zero);
				if (initialized == false)
				{
					Warn("No Tobii Eye Tracker was detected! Eye tracking will be unavailable for this session.");
				}
				else
				{
					Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
					harmony.PatchAll();
				}


			}
			catch (Exception e)
			{
				Warn("An unexpected error occured during engine initialization.");
				Error(e.Message);
			}
		}

		private static void setupDevice(string device, IntPtr intptr)
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

				Native.Tobii_wearable_consumer_data_subscribe(tobiiDevice, getEyeDataVR, IntPtr.Zero);
				Debug($"VR mode initiallized!");

				Native.Tobii_get_device_info(tobiiDevice, ref tobiiDeviceInfo);
			}
			catch (Exception e)
			{
				Warn("An unexpected error occured during Tobii device setup.");
				Error(e.Message);
			}
		}

		private unsafe static void getEyeDataVR(in Native.Tobii_wearable_consumer_data_t data, IntPtr user_data)
		{
			eyeDataVR = data;
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				Native.Tobii_wearable_consumer_data_unsubscribe(tobiiDevice);
				Native.Tobii_device_destroy(tobiiDevice);
				Native.Tobii_api_destroy(tobiiAPI);
				running = false;
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
					Debug($"Device stats: " +
	$"Firmware Version: {tobiiDeviceInfo.firmware_version}" +
	$"Model: {tobiiDeviceInfo.model}" +
	$"Runtime Build Version: {tobiiDeviceInfo.runtime_build_version}" +
	$"Serial Number: {tobiiDeviceInfo.serial_number}");

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
				EyeDataTreeDictionary.Add("Model", "Unknown (Vive Eye Devkit)");
				list.Add(EyeDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, "Tobii Eye Tracking");
			}

			public unsafe void UpdateInputs(float deltaTime)
			{
				eyes.IsEyeTrackingActive = initialized && Engine.Current.InputInterface.VR_Active;

				eyes.LeftEye.IsDeviceActive = initialized;
				eyes.RightEye.IsDeviceActive = initialized;
				eyes.CombinedEye.IsDeviceActive = initialized;

				eyes.LeftEye.IsTracking = Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsTracking = Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsTracking = eyeDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid;

				eyes.LeftEye.Openness = (int)eyeDataVR.left.blink == 1 ? 1f : 0f;
				eyes.RightEye.Openness = (int)eyeDataVR.right.blink == 1 ? 1f : 0f;
				eyes.CombinedEye.Openness = (int)eyeDataVR.left.blink == 1 || (int)eyeDataVR.right.blink == 1 ? 1f : 0f;

				eyes.CombinedEye.RawPosition = (eyeDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeDataVR.gaze_origin_combined_mm_xyz[0],
					eyeDataVR.gaze_origin_combined_mm_xyz[1],
					eyeDataVR.gaze_origin_combined_mm_xyz[2]) : float3.Zero;

				eyes.CombinedEye.Direction = (eyeDataVR.gaze_direction_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeDataVR.gaze_direction_combined_normalized_xyz[0],
					eyeDataVR.gaze_direction_combined_normalized_xyz[1],
					eyeDataVR.gaze_direction_combined_normalized_xyz[2]) : new float3(0, 0, 1);

				eyes.ConvergenceDistance = eyeDataVR.convergence_distance_validity == Native.Tobii_validity_t.ValidityValid
					? eyeDataVR.convergence_distance_mm : 0f;
				eyes.Timestamp = eyeDataVR.timestamp_us;
			}
		}
	}
}