using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using TobiiEyeBridge;
using System.Collections.Generic;

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

		// Screen
		public static Native.Tobii_gaze_point_t eyeDataScreenPoint;
		public static Native.Tobii_gaze_origin_t eyeDataScreenOrigin;
		public static bool isScreenUserPresent = true;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-screen-1.0.7";
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

				Native.Tobii_gaze_point_subscribe(tobiiDevice, getEyeDataScreenPoint, IntPtr.Zero);
				Native.Tobii_gaze_origin_subscribe(tobiiDevice, getEyeDataScreenOrigin, IntPtr.Zero);
				Native.Tobii_user_presence_subscribe(tobiiDevice, isUserPresent, IntPtr.Zero);
				Debug($"Screen mode initiallized!");

				Native.Tobii_get_device_info(tobiiDevice, ref tobiiDeviceInfo);
			}
			catch (Exception e)
			{
				Warn("An unexpected error occured during Tobii device setup.");
				Error(e.Message);
			}
		}

		private unsafe static void getEyeDataScreenOrigin(in Native.Tobii_gaze_origin_t data, IntPtr user_data)
		{
			eyeDataScreenOrigin = data;
		}

		private unsafe static void getEyeDataScreenPoint(in Native.Tobii_gaze_point_t data, IntPtr user_data)
		{
			eyeDataScreenPoint = data;
		}

		private unsafe static void isUserPresent(Native.Tobii_user_presence_status_t status, long timestamp_us, IntPtr user_data)
		{
			isScreenUserPresent = status == Native.Tobii_user_presence_status_t.UserPresenceStatusAway;
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				Native.Tobii_gaze_origin_unsubscribe(tobiiDevice);
				Native.Tobii_user_presence_unsubscribe(tobiiDevice);

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
				EyeDataTreeDictionary.Add("Model", "Tobii Eye 4C/5)");
				list.Add(EyeDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, "Tobii Eye Tracking");
			}

			// https://gamedev.stackexchange.com/questions/137305/need-help-with-getting-a-direction-vector-between-two-given-points
			public unsafe void UpdateInputs(float deltaTime)
			{
				eyes.IsEyeTrackingActive = initialized && !Engine.Current.InputInterface.VR_Active;

				eyes.LeftEye.IsDeviceActive = initialized;
				eyes.RightEye.IsDeviceActive = initialized;
				eyes.CombinedEye.IsDeviceActive = initialized;

				eyes.LeftEye.IsTracking = isScreenUserPresent;
				eyes.RightEye.IsTracking = isScreenUserPresent;
				eyes.CombinedEye.IsTracking = isScreenUserPresent;

				eyes.Timestamp = eyeDataScreenPoint.timestamp_us;

				var leftEyePos = new float3(
					eyeDataScreenOrigin.left_xyz[0],
					eyeDataScreenOrigin.left_xyz[1],
					eyeDataScreenOrigin.left_xyz[2]);
				var leftEyeDir = new float3(
					eyeDataScreenPoint.position_xy[0],
					eyeDataScreenPoint.position_xy[1],
					0f);
				eyes.LeftEye.RawPosition = leftEyePos;
				eyes.LeftEye.Openness = (eyeDataScreenOrigin.left_validity == Native.Tobii_validity_t.ValidityValid) ? 1f : 0f;
				eyes.LeftEye.Direction = (leftEyeDir - leftEyePos).Normalized;

				var rightEyePos = new float3(
					eyeDataScreenOrigin.right_xyz[0],
					eyeDataScreenOrigin.right_xyz[1],
					eyeDataScreenOrigin.right_xyz[2]);
				var rightEyeDir = new float3(
					eyeDataScreenPoint.position_xy[0],
					eyeDataScreenPoint.position_xy[1],
					0f);
				eyes.RightEye.RawPosition = rightEyePos;
				eyes.RightEye.Openness = (eyeDataScreenOrigin.right_validity == Native.Tobii_validity_t.ValidityValid) ? 1f : 0f;
				eyes.RightEye.Direction = (rightEyeDir - rightEyePos).Normalized;
			}
		}
    }
}