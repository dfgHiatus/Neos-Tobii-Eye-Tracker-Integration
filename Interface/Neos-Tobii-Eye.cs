using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using Tobii.XR;

namespace Neos_OpenSeeFace_Integration
{
	public class Neos_Tobii_Eye : NeosMod
	{
		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-Eye-Face-API/";
		public override void OnEngineInit()
		{
			// Harmony.DEBUG = true;
			var settings = new TobiiXR_Settings();
			TobiiXR.Start(settings);
			Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				TobiiXR.Stop();
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
			EyeDataTreeDictionary.Add("Name", "Generic Eye Tracking");
			EyeDataTreeDictionary.Add("Type", "Eye Tracking");
			EyeDataTreeDictionary.Add("Model", "Generic Eye Model");
			list.Add(EyeDataTreeDictionary);
		}

		public void RegisterInputs(InputInterface inputInterface)
		{
			eyes = new Eyes(inputInterface, "Generic Eye Tracking");
		}

		public void UpdateInputs(float deltaTime)
		{
			eyes.LeftEye.IsDeviceActive = eyeInt.LeftIsDeviceActive;
			eyes.RightEye.IsDeviceActive = eyeInt.RightIsDeviceActive;
			eyes.CombinedEye.IsDeviceActive = eyeInt.CombinedIsDeviceActive;

			eyes.LeftEye.IsTracking = eyeInt.LeftIsTracking;
			eyes.RightEye.IsTracking = eyeInt.RightIsDeviceActive;
			eyes.CombinedEye.IsTracking = eyeInt.CombinedIsDeviceActive;

			eyes.Timestamp = eyeInt.Timestamp;

			eyes.LeftEye.Squeeze = eyeInt.LeftSqueeze;
			eyes.RightEye.Squeeze = eyeInt.RightSqueeze;
			eyes.RightEye.Squeeze = eyeInt.CombinedSqueeze;

			eyes.LeftEye.Widen = eyeInt.LeftWiden;
			eyes.RightEye.Widen = eyeInt.RightWiden;
			eyes.CombinedEye.Widen = eyeInt.CombinedWiden;

			eyes.LeftEye.Frown = eyeInt.LeftFrown;
			eyes.RightEye.Frown = eyeInt.RightFrown;
			eyes.CombinedEye.Frown = eyeInt.CombinedFrown;

			eyes.LeftEye.Openness = eyeInt.LeftOpenness;
			eyes.RightEye.Openness = eyeInt.RightOpenness;
			eyes.CombinedEye.Openness = eyeInt.CombinedOpenness;

			eyes.LeftEye.PupilDiameter = eyeInt.LeftPupilDiameter;
			eyes.RightEye.PupilDiameter = eyeInt.RightPupilDiameter;
			eyes.CombinedEye.PupilDiameter = eyeInt.CombinedPupilDiameter;

		}
	}
}