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
		public static string deviceName;

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

				Debug($"Establishing connection to {deviceName}...");
				error = Native.Tobii_device_create(tobiiAPI, deviceName, Native.Tobii_field_of_use_t.FieldOfUseInteractive, out tobiiDevice);
				if (error != Native.Tobii_error_t.ErrorNoError)
					Debug("An error occured while connecting to this device. Error Code" + (int)error);

				// CONSUMER
				error = Native.Tobii_wearable_consumer_data_subscribe(tobiiDevice, getEyeDataVR, IntPtr.Zero);
				if (error != Native.Tobii_error_t.ErrorNoError)
					Debug("An error occured while connecting to this device. Error Code" + (int)error);


				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearable3dGazeCombined, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 3D Combined Gaze.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearable3dGazePerEye, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 3D Per Eye Gaze.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamUserPositionGuideXy, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 2D Per-Eye XY.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearablePupilPosition, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 2D Pupil XY.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamUserPositionGuideZ, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 2D Per-Eye Z.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearableEyeOpenness, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support 0.0-1.0 eye openess.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearablePupilDiameter, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support pupil dilation.");
				}
				Native.Tobii_capability_supported(tobiiDevice, Native.Tobii_capability_t.CapabilityCompoundStreamWearableConvergenceDistance, ref tobii_Capability);
				if (tobii_Capability != Native.Tobii_supported_t.Supported)
				{
					Warn($"This VR headset does not support convergence distance.");
				}

				Debug($"VR mode initiallized!");
				Native.Tobii_get_device_info(tobiiDevice, ref tobiiDeviceInfo);
				Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
				harmony.PatchAll();

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
				deviceName = device;

			}
			catch (Exception e)
			{
				Warn("An unexpected error occured during Tobii device setup.");
				Error(e.Message);
			}
		}

		private static void getEyeDataVR(in Native.Tobii_wearable_consumer_data_t data, IntPtr user_data)
		{
			eyeConsumerDataVR = data;
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				// CONSUMER
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
					Debug($"Current feature level:");
					Native.Tobii_get_feature_group(tobiiDevice, ref tobii_Feature);
					switch (tobii_Feature) {
						case Native.Tobii_feature_group_t.FeatureGroupConsumer:
							Debug("Consumer");
							break;
						case Native.Tobii_feature_group_t.FeatureGroupBlocked:
							Debug("Blocked");
							break;
						case Native.Tobii_feature_group_t.FeatureGroupConfig:
							Debug("Config");
							break;
						case Native.Tobii_feature_group_t.FeatureGroupInternal:
							Debug("Internal");
							break;
						case Native.Tobii_feature_group_t.FeatureGroupProfessional:
							Debug("Professional");
							break;
					}

					Debug($"Device stats: " +
					$"Firmware Version: {tobiiDeviceInfo.firmware_version}" +
					$"Model: {tobiiDeviceInfo.model}" +
					$"Lot ID: {tobiiDeviceInfo.lot_id}" +
					$"Generation: {tobiiDeviceInfo.generation}" +
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
				EyeDataTreeDictionary.Add("Model", "Vive Eye Devkit");
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
				eyes.CombinedEye.IsTracking = eyeConsumerDataVR.gaze_origin_combined_validity == Native.Tobii_validity_t.ValidityValid;

				eyes.LeftEye.Openness = (eyeConsumerDataVR.left.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;
				eyes.RightEye.Openness = (eyeConsumerDataVR.right.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;
				eyes.CombinedEye.Openness = (eyeConsumerDataVR.left.blink == Native.Tobii_state_bool_t.StateBoolTrue ||
					eyeConsumerDataVR.right.blink == Native.Tobii_state_bool_t.StateBoolTrue) ? 0f : 1f;

				if (eyeConsumerDataVR.left.pupil_position_in_sensor_area_validity == Native.Tobii_validity_t.ValidityValid &&
					eyeConsumerDataVR.right.pupil_position_in_sensor_area_validity == Native.Tobii_validity_t.ValidityValid)
				{
					eyes.CombinedEye.RawPosition = float3.Zero;

					eyes.CombinedEye.Direction = new float3(
												 MathX.Average(
													MathX.Tan(
														MathX.Remap(eyeConsumerDataVR.left.pupil_position_in_sensor_area_xy[1], 0, 1, -1, 1)), 
													MathX.Tan(
														MathX.Remap(eyeConsumerDataVR.right.pupil_position_in_sensor_area_xy[1], 0, 1, -1, 1))
												),
												MathX.Average(
													MathX.Tan(
														MathX.Remap(eyeConsumerDataVR.left.pupil_position_in_sensor_area_xy[0], 0, 1, -1, 1)), 
													MathX.Tan(
														MathX.Remap(eyeConsumerDataVR.right.pupil_position_in_sensor_area_xy[0], 0, 1, -1, 1))
												),
												1f).Normalized;
				}

				eyes.Timestamp = eyeConsumerDataVR.timestamp_us;
			}
		}
	}
}