using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		public override string Name => "Neos-Tobii-Screen-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";
		
		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.dfg.Tobii-Screen-Eye-Tracking");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
		[HarmonyPatch(new[] { typeof(Engine) })]
		public class InputInterfaceCtorPatch
		{
			public static void Postfix(InputInterface __instance)
			{
				try
				{
					if (TobiiCompanionInterface.Connect())
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
				EyeDataTreeDictionary.Add($"Model", $"{ TobiiCompanionInterface.gazeData.deviceInfo.name }");
				list.Add(EyeDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, $"Tobii Eye Tracking");
			}

			public void UpdateInputs(float deltaTime)
			{
				eyes.IsEyeTrackingActive = !Engine.Current.InputInterface.VR_Active;

				eyes.LeftEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.LeftEye.IsTracking = TobiiCompanionInterface.gazeData.leftEye.origin.validity == Validity.Valid;
				eyes.LeftEye.Direction = ((float3)new double3(MathX.Tan(MathX.Remap(TobiiCompanionInterface.gazeData.leftEye.direction.x, 0, 1, -1, 1)),
															  MathX.Tan(-MathX.Remap(TobiiCompanionInterface.gazeData.leftEye.direction.y, 0, 1, -1, 1)),
															  1f)).Normalized;
				eyes.LeftEye.RawPosition = ((float3)new double3(TobiiCompanionInterface.gazeData.leftEye.origin.x,
													   TobiiCompanionInterface.gazeData.leftEye.origin.y,
													   TobiiCompanionInterface.gazeData.leftEye.origin.z)).Normalized;
				eyes.LeftEye.PupilDiameter = 0.003f;
				eyes.LeftEye.Openness = 1f;
				eyes.LeftEye.Widen = 0f;
				eyes.LeftEye.Squeeze = 0f;
				eyes.LeftEye.Frown = 0f;

				eyes.RightEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.RightEye.IsTracking = TobiiCompanionInterface.gazeData.rightEye.origin.validity == Validity.Valid;
				eyes.RightEye.Direction = ((float3)new double3(MathX.Tan(MathX.Remap(TobiiCompanionInterface.gazeData.rightEye.direction.x, 0, 1, -1, 1)),
															  MathX.Tan(-MathX.Remap(TobiiCompanionInterface.gazeData.rightEye.direction.y, 0, 1, -1, 1)),
															  1f)).Normalized; 
				eyes.RightEye.RawPosition = ((float3)new double3(TobiiCompanionInterface.gazeData.rightEye.origin.x,
													   TobiiCompanionInterface.gazeData.rightEye.origin.y,
													   TobiiCompanionInterface.gazeData.rightEye.origin.z)).Normalized;
				eyes.RightEye.PupilDiameter = 0.003f;
				eyes.RightEye.Openness = 0f;
				eyes.RightEye.Widen = 0f;
				eyes.RightEye.Squeeze = 0f;
				eyes.RightEye.Frown = 0f;

/*				eyes.CombinedEye.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.CombinedEye.IsTracking = tobiiX.CombinedIsDeviceTracking;
				eyes.CombinedEye.Direction = tobiiX.CombinedEyeDirection;
				eyes.CombinedEye.RawPosition = tobiiX.CombinedEyeRawPosition;
				eyes.CombinedEye.PupilDiameter = 0.003f;
				eyes.CombinedEye.Openness = 1f;
				eyes.CombinedEye.Widen = 0f;
				eyes.CombinedEye.Squeeze = 0f;
				eyes.CombinedEye.Frown = 0f;*/

				eyes.Timestamp += 0;
			}
		}
    }
}