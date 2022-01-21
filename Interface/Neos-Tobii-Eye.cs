using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using Tobii.Research;

namespace Neos_OpenSeeFace_Integration
{
	public class Neos_Tobii_Eye : NeosMod
	{
		public static IEyeTracker eyeTracker;

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
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-Eye-Face-API/";
		public override void OnEngineInit()
		{
			// Harmony.DEBUG = true;
			eyeTracker.GazeDataReceived += EyeTracker_GazeDataReceived;
			Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
			harmony.PatchAll();
		}

		private static void EyeTracker_GazeDataReceived(object sender, GazeDataEventArgs e)
		{
			// TODO check if both are valid?

			leftIsValid = e.LeftEye.GazeOrigin.Validity == Validity.Valid;
			leftBlink = e.LeftEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			leftRawPupil = e.LeftEye.Pupil.PupilDiameter;

			if (e.LeftEye.GazeOrigin.Validity == Validity.Valid && e.LeftEye.GazePoint.Validity == Validity.Valid)
            {
				leftRawPos = new float3(
					e.LeftEye.GazeOrigin.PositionInUserCoordinates.X,
					e.LeftEye.GazeOrigin.PositionInUserCoordinates.Y,
					e.LeftEye.GazeOrigin.PositionInUserCoordinates.Z);
				leftRawRot = new float3(
					e.LeftEye.GazePoint.PositionInUserCoordinates.X,
					e.LeftEye.GazePoint.PositionInUserCoordinates.Y,
					e.LeftEye.GazePoint.PositionInUserCoordinates.Z);
			}

			rightIsValid = e.RightEye.GazeOrigin.Validity == Validity.Valid;
			rightBlink = e.RightEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			rightRawPupil = e.RightEye.Pupil.PupilDiameter;

			if (e.RightEye.GazeOrigin.Validity == Validity.Valid && e.RightEye.GazePoint.Validity == Validity.Valid)
			{
				rightRawPos = new float3(
					e.RightEye.GazeOrigin.PositionInUserCoordinates.X,
					e.RightEye.GazeOrigin.PositionInUserCoordinates.Y,
					e.RightEye.GazeOrigin.PositionInUserCoordinates.Z);
				rightRawRot = new float3(
					e.RightEye.GazePoint.PositionInUserCoordinates.X,
					e.RightEye.GazePoint.PositionInUserCoordinates.Y,
					e.RightEye.GazePoint.PositionInUserCoordinates.Z);
			}

			timestamp = e.DeviceTimeStamp;

		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				eyeTracker.GazeDataReceived -= EyeTracker_GazeDataReceived;
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
			eyes.LeftEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
			eyes.RightEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
			eyes.CombinedEye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;

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