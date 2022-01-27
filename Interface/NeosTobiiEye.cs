using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using System.Reflection;
using System.Threading;
using Tobii.Research;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		// TODO
		// Expose EyeTrackingOperations as *juicy* config vars
		// Possibly add calibration?

		public static TobiiProTrackingInterface tobiiProTrackingInterface = null;
		public static bool usingTobiiScreen = false;

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
		public override string Version => "alpha-1.0.6";
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
					if (usingTobiiScreen)
                    {
						tobiiProTrackingInterface.eyeTracker.GazeDataReceived += TobiiProTrackingInterface.Screen_OnGazeDataReceived;
                        Debug("Initializing Tobii Eye Tracking in Screen Mode...");
                    }
					else
                    {
						tobiiProTrackingInterface.eyeTracker.HMDGazeDataReceived += TobiiProTrackingInterface.HMD_OnGazeDataReceived;
                        Debug("Initializing Tobii Eye Tracking in VR Mode...");
                    }

					tobiiProTrackingInterface.eyeTracker.ConnectionLost += TobiiProTrackingInterface.EyeTracker_ConnectionLost;
					tobiiProTrackingInterface.eyeTracker.ConnectionRestored += TobiiProTrackingInterface.EyeTracker_ConnectionRestored;
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
					Warn("No Tobii eye tracker was found.");
					Warn("Tobii eye tracking will be unavailble for this session.");
				}
            }
			catch (ReflectionTypeLoadException)
			{
				Error("Couldn't find the Tobii DLLs in the Neos base directory.");
				Error("Please check that Tobii.Research.dll, tobii_pro.dll, and tobii_firmware_upgrade.dll are present.");
				Warn("Tobii eye tracking will be unavailble for this session.");
			}
			catch (Exception e)
            {
				Error("An unexpected error occured when trying to initiallize Tobii Eye Tracking.");
				Warn("Tobii eye tracking will be unavailble for this session.");
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

        public class TobiiProTrackingInterface
        {
            private Thread _worker = null;
            private CancellationTokenSource cts = new CancellationTokenSource();

            public IEyeTracker eyeTracker = null;

            public bool Initialize()
            {
                bool foundTrackers = false;
                int trackers = 0;
                foreach (var findAllEyeTracker in EyeTrackingOperations.FindAllEyeTrackers())
                    trackers++;
                if (trackers >= 1)
                    foundTrackers = true;
                return foundTrackers;
            }

            private void VerifyClosedThread()
            {
                if (_worker != null)
                {
                    if (_worker.IsAlive)
                        _worker.Abort();
                }
                cts = new CancellationTokenSource();
                _worker = null;
            }

            private static Validity VerifyCombinedEye(Validity left, Validity right)
            {
                if (left == Validity.Valid && right == Validity.Valid)
                    return Validity.Valid;
                else
                    return Validity.Invalid;
            }

            private static EyeData CreateCombinedEye(EyeData leftEye, EyeData rightEye)
            {
                // BEGIN GAZEPOINT
                // NormalizedPoint2D x and y
                float gp_np2dx = (leftEye.GazePoint.PositionOnDisplayArea.X + rightEye.GazePoint.PositionOnDisplayArea.X) / 2;
                float gp_np2dy = (leftEye.GazePoint.PositionOnDisplayArea.Y + rightEye.GazePoint.PositionOnDisplayArea.Y) / 2;
                NormalizedPoint2D gp_np2d = new NormalizedPoint2D(gp_np2dx, gp_np2dy);
                // Point3D x y and z (cheated with float3.Cross(), how evil!)
                float3 gp_leftEyePoint3D = new float3(leftEye.GazePoint.PositionInUserCoordinates.X, leftEye.GazePoint.PositionInUserCoordinates.Y,
                    leftEye.GazePoint.PositionInUserCoordinates.Z);
                float3 gp_rightEyePoint3D = new float3(rightEye.GazePoint.PositionInUserCoordinates.X, rightEye.GazePoint.PositionInUserCoordinates.Y,
                    rightEye.GazePoint.PositionInUserCoordinates.Z);
                float3 gp_crossedEyePoints = MathX.Cross(gp_leftEyePoint3D, gp_rightEyePoint3D);
                Point3D p3d = new Point3D(gp_crossedEyePoints.x, gp_crossedEyePoints.y, gp_crossedEyePoints.z);
                GazePoint gazePoint = new GazePoint(gp_np2d, p3d,
                    VerifyCombinedEye(leftEye.GazePoint.Validity, rightEye.GazePoint.Validity));
                // END GAZEPOINT
                // BEGIN PUPIL
                PupilData pupilData = new PupilData((leftEye.Pupil.PupilDiameter + rightEye.Pupil.PupilDiameter) / 2,
                    VerifyCombinedEye(leftEye.Pupil.Validity, rightEye.Pupil.Validity));
                // END PUPIL
                // BEGIN GAZEORIGIN
                // Point3D x y and z (cheated with float3.Cross(), how diabolical!)
                float3 go_leftEyePoint3D_uc = new float3(leftEye.GazeOrigin.PositionInUserCoordinates.X,
                    leftEye.GazeOrigin.PositionInUserCoordinates.Y, leftEye.GazeOrigin.PositionInUserCoordinates.Z);
                float3 go_rightEyePoint3D_uc = new float3(rightEye.GazeOrigin.PositionInUserCoordinates.X,
                    rightEye.GazeOrigin.PositionInUserCoordinates.Y, rightEye.GazeOrigin.PositionInUserCoordinates.Z);
                float3 go_crossedEyePoints_uc = MathX.Cross(go_leftEyePoint3D_uc, go_rightEyePoint3D_uc);
                Point3D go_positionInUserCoordinates = new Point3D(go_crossedEyePoints_uc.x, go_crossedEyePoints_uc.y,
                    go_crossedEyePoints_uc.z);
                // NormalizedPoint3D x y and z
                float3 go_leftEyeNormalizedPoint3D_bc = new float3(leftEye.GazeOrigin.PositionInTrackBoxCoordinates.X,
                    leftEye.GazeOrigin.PositionInTrackBoxCoordinates.Y, leftEye.GazeOrigin.PositionInTrackBoxCoordinates.Z);
                float3 go_rightEyeNormalizedPoint3D_bc = new float3(rightEye.GazeOrigin.PositionInTrackBoxCoordinates.X,
                    rightEye.GazeOrigin.PositionInTrackBoxCoordinates.Y, rightEye.GazeOrigin.PositionInTrackBoxCoordinates.Z);
                float3 go_crossedEyePoints_bc =
                    MathX.Cross(go_leftEyeNormalizedPoint3D_bc, go_rightEyeNormalizedPoint3D_bc);
                NormalizedPoint3D go_positionInTrackBoxCoordinates = new NormalizedPoint3D(go_crossedEyePoints_bc.x,
                    go_crossedEyePoints_bc.y, go_crossedEyePoints_bc.z);
                GazeOrigin gazeOrigin = new GazeOrigin(go_positionInUserCoordinates, go_positionInTrackBoxCoordinates,
                    VerifyCombinedEye(leftEye.GazeOrigin.Validity, rightEye.GazeOrigin.Validity));
                // END GAZEORIGIN
                EyeData neosEye = new EyeData(gazePoint, pupilData, gazeOrigin);
                return neosEye;
            }

            private static EyeData CreateCombinedEye(HMDEyeData leftEye, HMDEyeData rightEye)
            {
                // BEGIN GAZEPOINT
                // NormalizedPoint2D x and y
                float gp_np2dx = (leftEye.GazeOrigin.PositionInHMDCoordinates.X + rightEye.GazeOrigin.PositionInHMDCoordinates.X) / 2;
                float gp_np2dy = (leftEye.GazeOrigin.PositionInHMDCoordinates.Y + rightEye.GazeOrigin.PositionInHMDCoordinates.Y) / 2;
                NormalizedPoint2D gp_np2d = new NormalizedPoint2D(gp_np2dx, gp_np2dy);
                // Point3D x y and z (cheated with float3.Cross(), how evil!)
                float3 gp_leftEyePoint3D = new float3(leftEye.GazeOrigin.PositionInHMDCoordinates.X, leftEye.GazeOrigin.PositionInHMDCoordinates.Y,
                    leftEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 gp_rightEyePoint3D = new float3(rightEye.GazeOrigin.PositionInHMDCoordinates.X, rightEye.GazeOrigin.PositionInHMDCoordinates.Y,
                    rightEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 gp_crossedEyePoints = MathX.Cross(gp_leftEyePoint3D, gp_rightEyePoint3D);
                Point3D p3d = new Point3D(gp_crossedEyePoints.x, gp_crossedEyePoints.y, gp_crossedEyePoints.z);
                GazePoint gazePoint = new GazePoint(gp_np2d, p3d,
                    VerifyCombinedEye(leftEye.GazeOrigin.Validity, rightEye.GazeOrigin.Validity));
                // END GAZEPOINT
                // BEGIN PUPIL
                PupilData pupilData = new PupilData((leftEye.Pupil.PupilDiameter + rightEye.Pupil.PupilDiameter) / 2,
                    VerifyCombinedEye(leftEye.Pupil.Validity, rightEye.Pupil.Validity));
                // END PUPIL
                // BEGIN GAZEORIGIN
                // Point3D x y and z (cheated with float3.Cross(), how diabolical!)
                float3 go_leftEyePoint3D_uc = new float3(leftEye.GazeOrigin.PositionInHMDCoordinates.X,
                    leftEye.GazeOrigin.PositionInHMDCoordinates.Y, leftEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 go_rightEyePoint3D_uc = new float3(rightEye.GazeOrigin.PositionInHMDCoordinates.X,
                    rightEye.GazeOrigin.PositionInHMDCoordinates.Y, rightEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 go_crossedEyePoints_uc = MathX.Cross(go_leftEyePoint3D_uc, go_rightEyePoint3D_uc);
                Point3D go_positionInUserCoordinates = new Point3D(go_crossedEyePoints_uc.x, go_crossedEyePoints_uc.y,
                    go_crossedEyePoints_uc.z);
                // NormalizedPoint3D x y and z
                float3 go_leftEyeNormalizedPoint3D_bc = new float3(leftEye.GazeOrigin.PositionInHMDCoordinates.X,
                    leftEye.GazeOrigin.PositionInHMDCoordinates.Y, leftEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 go_rightEyeNormalizedPoint3D_bc = new float3(rightEye.GazeOrigin.PositionInHMDCoordinates.X,
                    rightEye.GazeOrigin.PositionInHMDCoordinates.Y, rightEye.GazeOrigin.PositionInHMDCoordinates.Z);
                float3 go_crossedEyePoints_bc =
                    MathX.Cross(go_leftEyeNormalizedPoint3D_bc, go_rightEyeNormalizedPoint3D_bc);
                NormalizedPoint3D go_positionInTrackBoxCoordinates = new NormalizedPoint3D(go_crossedEyePoints_bc.x,
                    go_crossedEyePoints_bc.y, go_crossedEyePoints_bc.z);
                GazeOrigin gazeOrigin = new GazeOrigin(go_positionInUserCoordinates, go_positionInTrackBoxCoordinates,
                    VerifyCombinedEye(leftEye.GazeOrigin.Validity, rightEye.GazeOrigin.Validity));
                // END GAZEORIGIN
                EyeData neosEye = new EyeData(gazePoint, pupilData, gazeOrigin);
                return neosEye;
            }

            public static void Screen_OnGazeDataReceived(object sender, GazeDataEventArgs gazeDataEventArgs)
            {
                EyeData combinedEye = CreateCombinedEye(gazeDataEventArgs.LeftEye,
                     gazeDataEventArgs.RightEye);

                // Update Eye Tracker Data
                leftIsValid = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazePoint.Validity == Validity.Valid;
                leftBlink = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazePoint.Validity == Validity.Valid ? 1f : 0f;
                leftRawPos = new float3(
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInUserCoordinates.X,
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInUserCoordinates.Y,
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInUserCoordinates.Z);
                leftRawRot = new float3(
                        gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.X,
                        gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.Y,
                        gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.Z);
                leftRawPupil = gazeDataEventArgs.LeftEye.Pupil.PupilDiameter;

                rightIsValid = gazeDataEventArgs.RightEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.RightEye.GazePoint.Validity == Validity.Valid;
                rightBlink = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazePoint.Validity == Validity.Valid ? 1f : 0f;
                rightRawPos = new float3(
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInUserCoordinates.X,
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInUserCoordinates.Y,
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInUserCoordinates.Z);
                rightRawRot = new float3(
                        gazeDataEventArgs.RightEye.GazePoint.PositionInUserCoordinates.X,
                        gazeDataEventArgs.RightEye.GazePoint.PositionInUserCoordinates.Y,
                        gazeDataEventArgs.RightEye.GazePoint.PositionInUserCoordinates.Z);
                rightRawPupil = gazeDataEventArgs.RightEye.Pupil.PupilDiameter;

                combinedIsValid = combinedEye.GazeOrigin.Validity == Validity.Valid && combinedEye.GazePoint.Validity == Validity.Valid;
                combinedBlink = combinedEye.GazeOrigin.Validity == Validity.Valid && combinedEye.GazePoint.Validity == Validity.Valid ? 1f : 0f;
                combinedRawPos = new float3(
                        combinedEye.GazeOrigin.PositionInUserCoordinates.X,
                        combinedEye.GazeOrigin.PositionInUserCoordinates.Y,
                        combinedEye.GazeOrigin.PositionInUserCoordinates.Z);
                combinedRawRot = new float3(
                        combinedEye.GazePoint.PositionInUserCoordinates.X,
                        combinedEye.GazePoint.PositionInUserCoordinates.Y,
                        combinedEye.GazePoint.PositionInUserCoordinates.Z);
                combinedRawPupil = combinedEye.Pupil.PupilDiameter;
            }

            public static void HMD_OnGazeDataReceived(object sender, HMDGazeDataEventArgs gazeDataEventArgs)
            {
                EyeData combinedEye = CreateCombinedEye(gazeDataEventArgs.LeftEye,
                     gazeDataEventArgs.RightEye);

                // Update Eye Tracker Data
                leftIsValid = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazeDirection.Validity == Validity.Valid;
                leftBlink = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazeDirection.Validity == Validity.Valid ? 1f : 0f;
                leftRawPos = new float3(
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInHMDCoordinates.X,
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInHMDCoordinates.Y,
                        gazeDataEventArgs.LeftEye.GazeOrigin.PositionInHMDCoordinates.Z);
                leftRawRot = new float3(
                        gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.X,
                        gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.Y,
                        gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.Z);
                leftRawPupil = gazeDataEventArgs.LeftEye.Pupil.PupilDiameter;

                rightIsValid = gazeDataEventArgs.RightEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.RightEye.GazeDirection.Validity == Validity.Valid;
                rightBlink = gazeDataEventArgs.LeftEye.GazeOrigin.Validity == Validity.Valid && gazeDataEventArgs.LeftEye.GazeDirection.Validity == Validity.Valid ? 1f : 0f;
                rightRawPos = new float3(
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInHMDCoordinates.X,
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInHMDCoordinates.Y,
                        gazeDataEventArgs.RightEye.GazeOrigin.PositionInHMDCoordinates.Z);
                rightRawRot = new float3(
                        gazeDataEventArgs.RightEye.GazeDirection.UnitVector.X,
                        gazeDataEventArgs.RightEye.GazeDirection.UnitVector.Y,
                        gazeDataEventArgs.RightEye.GazeDirection.UnitVector.Z);
                rightRawPupil = gazeDataEventArgs.RightEye.Pupil.PupilDiameter;

                combinedIsValid = combinedEye.GazeOrigin.Validity == Validity.Valid && combinedEye.GazePoint.Validity == Validity.Valid;
                combinedBlink = combinedEye.GazeOrigin.Validity == Validity.Valid && combinedEye.GazePoint.Validity == Validity.Valid ? 1f : 0f;
                combinedRawPos = new float3(
                        combinedEye.GazeOrigin.PositionInUserCoordinates.X,
                        combinedEye.GazeOrigin.PositionInUserCoordinates.Y,
                        combinedEye.GazeOrigin.PositionInUserCoordinates.Z);
                combinedRawRot = new float3(
                        combinedEye.GazePoint.PositionInUserCoordinates.X,
                        combinedEye.GazePoint.PositionInUserCoordinates.Y,
                        combinedEye.GazePoint.PositionInUserCoordinates.Z);
                combinedRawPupil = combinedEye.Pupil.PupilDiameter;
            }

            public static void EyeTracker_ConnectionLost(object sender, ConnectionLostEventArgs e)
            {
                Debug("Connection was lost to the Tobii eye tracking service.");
            }

            public static void EyeTracker_ConnectionRestored(object sender, ConnectionRestoredEventArgs e)
            {
                Debug("Connection was restored to the Tobii eye tracking service.");
            }

            public void StartThread()
            {
                VerifyClosedThread();
                _worker = new Thread(() =>
                {
                    // For now, just going to pick the first eye tracker found
                    // Later on when a Config is added, we'll allow a user to enter a URI
                    EyeTrackerCollection etc = EyeTrackingOperations.FindAllEyeTrackers();
                    eyeTracker = etc[0];
                    if (usingTobiiScreen)
                    {
                        eyeTracker.GazeDataReceived += Screen_OnGazeDataReceived;
                    }
                    else
                    {
                        eyeTracker.HMDGazeDataReceived += HMD_OnGazeDataReceived;
                    }
                    eyeTracker.ConnectionLost += EyeTracker_ConnectionLost;
                    eyeTracker.ConnectionRestored += EyeTracker_ConnectionRestored;
                    while (!cts.IsCancellationRequested)
                    {
                        // Events handle Eye Data
                        Thread.Sleep(10);
                    }
                });
                _worker.Start();
            }

            public void Teardown()
            {
                if (usingTobiiScreen)
                {
                    eyeTracker.GazeDataReceived -= Screen_OnGazeDataReceived;
                }
                else
                {
                    eyeTracker.HMDGazeDataReceived -= HMD_OnGazeDataReceived;
                }
                eyeTracker.ConnectionLost -= EyeTracker_ConnectionLost;
                eyeTracker.ConnectionRestored -= EyeTracker_ConnectionRestored;
                EyeTrackingOperations.Terminate();
                _worker.Abort();
            }

            // Empty as it's not needed
            public void Update() { }
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
			eyes.CombinedEye.Squeeze = 0f;

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