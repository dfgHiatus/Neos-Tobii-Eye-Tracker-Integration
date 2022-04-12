using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using EyeXFramework;
using Tobii.EyeX.Framework;
using System.Threading;
using TobiiAPI;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		public override string Name => "Neos-Tobii-Screen-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

		private static TobiiScreen tobiiX;
		
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.dfg.Tobii-Screen-Eye-Tracking");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				tobiiX.Teardown();
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
					tobiiX = new TobiiScreen();
					if (tobiiX.Init())
                    {
						GenericInputDevice gen = new GenericInputDevice();
						Debug("Module Name: " + gen.ToString());
						__instance.RegisterInputDriver(gen);
                    }
					else
                    {
						throw new InvalidOperationException("Could not create a Tobii Eye Tracking session.");
					}

				}
				catch (InvalidOperationException e)
				{
					Warn(e.Message);
				}
				catch (Exception e)
				{
					Warn($"Module failed to initiallize with the following error.");
					Warn("Error Type:" + e.GetType().ToString());
					Warn(e.Message);
				}
			}
		}

		public class GenericInputDevice : IInputDriver
		{
			public Eyes eyes;
			public int UpdateOrder => 100;

			public void CollectDeviceInfos(DataTreeList list)
			{
				DataTreeDictionary EyeDataTreeDictionary = new DataTreeDictionary();
				EyeDataTreeDictionary.Add($"Name", $"Tobii Eye Tracking");
				EyeDataTreeDictionary.Add($"Type", $"Eye Tracking");
				EyeDataTreeDictionary.Add($"Model", $"{ tobiiX.deviceName }");
				list.Add(EyeDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, $"Tobii Eye Tracking");
			}

			public void UpdateInputs(float deltaTime)
			{
				// Because other threads are iffy?
				tobiiX.Update();

				eyes.IsEyeTrackingActive = !Engine.Current.InputInterface.VR_Active;
				eyes.LeftEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;

				eyes.LeftEye.IsTracking = tobiiX.LeftIsDeviceTracking;
				eyes.LeftEye.Direction = tobiiX.LeftEyeDirection;
				eyes.LeftEye.RawPosition = tobiiX.LeftEyeRawPosition;
				eyes.LeftEye.PupilDiameter = tobiiX.LeftPupilDiameter;
				eyes.LeftEye.Openness = tobiiX.LeftOpenness;
				eyes.LeftEye.Widen = tobiiX.LeftWiden;
				eyes.LeftEye.Squeeze = tobiiX.LeftSqueeze;
				eyes.LeftEye.Frown = tobiiX.LeftFrown;

				eyes.RightEye.IsTracking = tobiiX.RightIsDeviceTracking;
				eyes.RightEye.Direction = tobiiX.RightEyeDirection;
				eyes.RightEye.RawPosition = tobiiX.RightEyeRawPosition;
				eyes.RightEye.PupilDiameter = tobiiX.RightPupilDiameter;
				eyes.RightEye.Openness = tobiiX.RightOpenness;
				eyes.RightEye.Widen = tobiiX.RightWiden;
				eyes.RightEye.Squeeze = tobiiX.RightSqueeze;
				eyes.RightEye.Frown = tobiiX.RightFrown;

				eyes.CombinedEye.IsTracking = tobiiX.CombinedIsDeviceTracking;
				eyes.CombinedEye.Direction = tobiiX.CombinedEyeDirection;
				eyes.CombinedEye.RawPosition = tobiiX.CombinedEyeRawPosition;
				eyes.CombinedEye.PupilDiameter = tobiiX.CombinedPupilDiameter;
				eyes.CombinedEye.Openness = tobiiX.CombinedOpenness;
				eyes.CombinedEye.Widen = tobiiX.CombinedWiden;
				eyes.CombinedEye.Squeeze = tobiiX.CombinedSqueeze;
				eyes.CombinedEye.Frown = tobiiX.CombinedFrown;

				eyes.Timestamp += 0;
			}
		}
    }
}