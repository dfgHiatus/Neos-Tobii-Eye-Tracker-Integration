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
		public static Native.Tobii_supported_t tobii_Capability;
		public static Native.Tobii_feature_group_t tobii_Feature;
		public static Native.Tobii_error_t error;

		// VR
		public static Native.Tobii_wearable_consumer_data_t eyeConsumerDataVR; // CONSUMER
		public static Native.Tobii_wearable_advanced_data_t eyeAdvancedDataVR;

		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-vr-1.0.7";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		public override void OnEngineInit()
		{
			try
			{
				error = Native.Tobii_api_create(out tobiiAPI, IntPtr.Zero, IntPtr.Zero);
				error = Native.Tobii_enumerate_local_device_urls(tobiiAPI, setupDevice, IntPtr.Zero);
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
				error = Native.Tobii_device_create(tobiiAPI, device, Native.Tobii_field_of_use_t.FieldOfUseInteractive, out tobiiDevice);
				if (error != Native.Tobii_error_t.ErrorNoError)
					Debug("An error occured while connecting to this device. Error Code" + (int)error);
				error = Native.Tobii_wearable_advanced_data_subscribe(tobiiDevice, getEyeDataVR, IntPtr.Zero);
				if (error != Native.Tobii_error_t.ErrorNoError)
					Debug("An error occured while connecting to this device. Error Code" + (int)error);
				// CONSUMER
				// Native.Tobii_wearable_consumer_data_subscribe(tobiiDevice, getEyeDataVR, IntPtr.Zero);

				bool isValidHMD = true;
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearable3dGazeCombined, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 3D Combined Gaze.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearable3dGazePerEye, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 3D Per Eye Gaze.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamUserPositionGuideXy, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 2D Per-Eye XY.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamUserPositionGuideZ, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 2D Per-Eye Z.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearableEyeOpenness, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 0.0-1.0 eye openess.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearablePupilDiameter, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support pupil dilation.");
					isValidHMD = false;
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearableConvergenceDistance, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support convergence distance.");
					isValidHMD = false;
				}

				if (isValidHMD)
				{
					Debug($"VR mode initiallized!");
					Native.Tobii_get_device_info(tobiiDevice, ref tobiiDeviceInfo);
					Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
					harmony.PatchAll();
				}
				else
				{
					Warn($"VR Device is not valid (yet) for setup.");
				}		
			}
			catch (Exception e)
			{
				Warn("An unexpected error occured during Tobii device setup.");
				Error(e.Message);
			}
		}

		private unsafe static void getEyeDataVR(in Native.Tobii_wearable_consumer_data_t data, IntPtr user_data)
		{
			eyeConsumerDataVR = data;
		}

		private unsafe static void getEyeDataVR(in Native.Tobii_wearable_advanced_data_t data, IntPtr user_data)
		{
			eyeAdvancedDataVR = data;
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				Native.Tobii_wearable_advanced_data_unsubscribe(tobiiDevice);
				// CONSUMER
				// Native.Tobii_wearable_consumer_data_unsubscribe(tobiiDevice);
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

					Native.Tobii_get_feature_group(tobiiDevice, ref tobii_Feature);
					Debug($"Current feature level {(tobii_Feature == Native.Tobii_feature_group_t.FeatureGroupConsumer ? "Consumer" : "Other")}");

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
				eyes.CombinedEye.IsTracking = eyeAdvancedDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid;

				eyes.LeftEye.Openness = (eyeAdvancedDataVR.left.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;
				eyes.RightEye.Openness = (eyeAdvancedDataVR.right.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;
				eyes.CombinedEye.Openness = (eyeAdvancedDataVR.left.blink == Native.Tobii_state_bool_t.StateBoolTrue || 
					eyeAdvancedDataVR.right.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;

				// ADVANCED
				eyes.LeftEye.RawPosition = (eyeAdvancedDataVR.left.gaze_direction_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.left.gaze_origin_mm_xyz[0],
					eyeAdvancedDataVR.left.gaze_origin_mm_xyz[1],
					eyeAdvancedDataVR.left.gaze_origin_mm_xyz[2]) : float3.Zero;

				eyes.LeftEye.Direction = (eyeAdvancedDataVR.left.gaze_direction_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.left.gaze_direction_normalized_xyz[0],
					eyeAdvancedDataVR.left.gaze_direction_normalized_xyz[1],
					eyeAdvancedDataVR.left.gaze_direction_normalized_xyz[2]) : new float3(0, 0, 1);

				eyes.RightEye.RawPosition = (eyeAdvancedDataVR.right.gaze_direction_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.right.gaze_origin_mm_xyz[0],
					eyeAdvancedDataVR.right.gaze_origin_mm_xyz[1],
					eyeAdvancedDataVR.right.gaze_origin_mm_xyz[2]) : float3.Zero;

				eyes.RightEye.Direction = (eyeAdvancedDataVR.right.gaze_direction_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.right.gaze_direction_normalized_xyz[0],
					eyeAdvancedDataVR.right.gaze_direction_normalized_xyz[1],
					eyeAdvancedDataVR.right.gaze_direction_normalized_xyz[2]) : new float3(0, 0, 1);

				eyes.CombinedEye.RawPosition = (eyeAdvancedDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.gaze_origin_combined_mm_xyz[0],
					eyeAdvancedDataVR.gaze_origin_combined_mm_xyz[1],
					eyeAdvancedDataVR.gaze_origin_combined_mm_xyz[2]) : float3.Zero;

				eyes.CombinedEye.Direction = (eyeAdvancedDataVR.gaze_direction_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeAdvancedDataVR.gaze_direction_combined_normalized_xyz[0],
					eyeAdvancedDataVR.gaze_direction_combined_normalized_xyz[1],
					eyeAdvancedDataVR.gaze_direction_combined_normalized_xyz[2]) : new float3(0, 0, 1);

				eyes.LeftEye.PupilDiameter = eyeAdvancedDataVR.left.pupil_diameter_mm;
				eyes.RightEye.PupilDiameter = eyeAdvancedDataVR.right.pupil_diameter_mm;
				eyes.CombinedEye.PupilDiameter = MathX.Average(eyeAdvancedDataVR.left.pupil_diameter_mm, eyeAdvancedDataVR.right.pupil_diameter_mm);

				eyes.ConvergenceDistance = eyeAdvancedDataVR.convergence_distance_validity == Native.Tobii_validity_t.ValidityValid
					? eyeAdvancedDataVR.convergence_distance_mm : 0f;
				eyes.Timestamp = eyeAdvancedDataVR.timestamp_tracker_us;

				// CONSUMER
/*					eyes.CombinedEye.RawPosition = (eyeDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeDataVR.gaze_origin_combined_mm_xyz[0],
					eyeDataVR.gaze_origin_combined_mm_xyz[1],
					eyeDataVR.gaze_origin_combined_mm_xyz[2]) : float3.Zero;

				eyes.CombinedEye.Direction = (eyeDataVR.gaze_direction_combined_validity == Native.Tobii_validity_t.ValidityValid) ? new float3(
					eyeDataVR.gaze_direction_combined_normalized_xyz[0],
					eyeDataVR.gaze_direction_combined_normalized_xyz[1],
					eyeDataVR.gaze_direction_combined_normalized_xyz[2]) : new float3(0, 0, 1);

				eyes.ConvergenceDistance = eyeDataVR.convergence_distance_validity == Native.Tobii_validity_t.ValidityValid
					? eyeDataVR.convergence_distance_mm : 0f;
				eyes.Timestamp = eyeDataVR.timestamp_us;*/
			}
		}
	}
}