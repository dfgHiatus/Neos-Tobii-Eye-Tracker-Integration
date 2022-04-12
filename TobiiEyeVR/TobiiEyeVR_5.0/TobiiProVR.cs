using System;
using System.Threading;
using Tobii.Research;

namespace TobiiProTrackingInterface
{
    public class TobiiProVR
    {
        private Thread _worker = null;
        private CancellationTokenSource cts = new CancellationTokenSource();
        public HMDGazeDataEventArgs gazeData;

        private IEyeTracker eyeTracker = null;
        
        public bool SupportsEye => true;
        public bool SupportsLip => false;

        public (bool eyeSuccess, bool lipSuccess) Initialize(bool eye, bool lip)
        {
            bool foundTrackers = false;
            int trackers = 0;
            foreach (var findAllEyeTracker in EyeTrackingOperations.FindAllEyeTrackers())
                trackers++;
            if (trackers >= 1)
                foundTrackers = true;
            return (foundTrackers && eye, false);
        }

        private void VerifyClosedThread()
        {
            if (_worker != null)
            {
                if (_worker.IsAlive)
                    Console.WriteLine("Eye data stream detected. Closing...");
                    _worker.Abort();
            }
            cts = new CancellationTokenSource();
            _worker = null;
        }

        private Validity VerifyCombinedEye(Validity left, Validity right)
        {
            if (left == Validity.Valid && right == Validity.Valid)
                return Validity.Valid;
            else
                return Validity.Invalid;
        }

       /* private EyeData CreateCombinedEye(EyeData leftEye, EyeData rightEye)
        {
            // BEGIN GAZEPOINT
            // NormalizedPoint2D x and y
            float gp_np2dx = (leftEye.GazePoint.PositionOnDisplayArea.X + rightEye.GazePoint.PositionOnDisplayArea.X) / 2;
            float gp_np2dy = (leftEye.GazePoint.PositionOnDisplayArea.Y + rightEye.GazePoint.PositionOnDisplayArea.Y) / 2;
            NormalizedPoint2D gp_np2d = new NormalizedPoint2D(gp_np2dx, gp_np2dy);
            // Point3D x y and z (cheated with Vector3.Cross(), how evil!)
            Vector3 gp_leftEyePoint3D = new Vector3(leftEye.GazePoint.PositionInUserCoordinates.X, leftEye.GazePoint.PositionInUserCoordinates.Y,
                leftEye.GazePoint.PositionInUserCoordinates.Z);
            Vector3 gp_rightEyePoint3D = new Vector3(rightEye.GazePoint.PositionInUserCoordinates.X, rightEye.GazePoint.PositionInUserCoordinates.Y,
                rightEye.GazePoint.PositionInUserCoordinates.Z);
            Vector3 gp_crossedEyePoints = Vector3.Cross(gp_leftEyePoint3D, gp_rightEyePoint3D);
            Point3D p3d = new Point3D(gp_crossedEyePoints.x, gp_crossedEyePoints.y, gp_crossedEyePoints.z);
            GazePoint gazePoint = new GazePoint(gp_np2d, p3d,
                VerifyCombinedEye(leftEye.GazePoint.Validity, rightEye.GazePoint.Validity));
            // END GAZEPOINT
            // BEGIN PUPIL
            PupilData pupilData = new PupilData((leftEye.Pupil.PupilDiameter + rightEye.Pupil.PupilDiameter) / 2,
                VerifyCombinedEye(leftEye.Pupil.Validity, rightEye.Pupil.Validity));
            // END PUPIL
            // BEGIN GAZEORIGIN
            // Point3D x y and z (cheated with Vector3.Cross(), how diabolical!)
            Vector3 go_leftEyePoint3D_uc = new Vector3(leftEye.GazeOrigin.PositionInUserCoordinates.X,
                leftEye.GazeOrigin.PositionInUserCoordinates.Y, leftEye.GazeOrigin.PositionInUserCoordinates.Z);
            Vector3 go_rightEyePoint3D_uc = new Vector3(rightEye.GazeOrigin.PositionInUserCoordinates.X,
                rightEye.GazeOrigin.PositionInUserCoordinates.Y, rightEye.GazeOrigin.PositionInUserCoordinates.Z);
            Vector3 go_crossedEyePoints_uc = Vector3.Cross(go_leftEyePoint3D_uc, go_rightEyePoint3D_uc);
            Point3D go_positionInUserCoordinates = new Point3D(go_crossedEyePoints_uc.x, go_crossedEyePoints_uc.y,
                go_crossedEyePoints_uc.z);
            // NormalizedPoint3D x y and z
            Vector3 go_leftEyeNormalizedPoint3D_bc = new Vector3(leftEye.GazeOrigin.PositionInTrackBoxCoordinates.X,
                leftEye.GazeOrigin.PositionInTrackBoxCoordinates.Y, leftEye.GazeOrigin.PositionInTrackBoxCoordinates.Z);
            Vector3 go_rightEyeNormalizedPoint3D_bc = new Vector3(rightEye.GazeOrigin.PositionInTrackBoxCoordinates.X,
                rightEye.GazeOrigin.PositionInTrackBoxCoordinates.Y, rightEye.GazeOrigin.PositionInTrackBoxCoordinates.Z);
            Vector3 go_crossedEyePoints_bc =
                Vector3.Cross(go_leftEyeNormalizedPoint3D_bc, go_rightEyeNormalizedPoint3D_bc);
            NormalizedPoint3D go_positionInTrackBoxCoordinates = new NormalizedPoint3D(go_crossedEyePoints_bc.x,
                go_crossedEyePoints_bc.y, go_crossedEyePoints_bc.z);
            GazeOrigin gazeOrigin = new GazeOrigin(go_positionInUserCoordinates, go_positionInTrackBoxCoordinates,
                VerifyCombinedEye(leftEye.GazeOrigin.Validity, rightEye.GazeOrigin.Validity));
            // END GAZEORIGIN
            EyeData combinedEye = new EyeData(gazePoint, pupilData, gazeOrigin);
            return combinedEye;
        }*/

        private void OnHMDDataReceived(object sender, HMDGazeDataEventArgs gazeDataEventArgs)
        {
            Console.WriteLine("OnHMDDataReceived");
            gazeData = gazeDataEventArgs;
        }

        public void StartThread()
        {
            // TODO
            Console.WriteLine("Testing if eye data is already open...");
            VerifyClosedThread();
            _worker = new Thread(() =>
            {
                Console.WriteLine("No stream detected. Starting new eye data stream...");

                // For now, just going to pick the first eye tracker found
                // Later on when a Config is added, we'll allow a user to enter a URI
                EyeTrackerCollection etc = EyeTrackingOperations.FindAllEyeTrackers();
                eyeTracker = etc[0];
                if (eyeTracker != null)
                {
                    Console.WriteLine("Device Capabilities:");
                    switch (eyeTracker.DeviceCapabilities)
                    {
                        case Capabilities.CanDoHMDBasedCalibration:
                            Console.WriteLine("CanDoHMDBasedCalibration");
                            break;
                        case Capabilities.CanDoMonocularCalibration:
                            Console.WriteLine("CanDoMonocularCalibration");
                            break;
                        case Capabilities.CanDoScreenBasedCalibration:
                            Console.WriteLine("CanDoScreenBasedCalibration");
                            break;
                        case Capabilities.CanSetDisplayArea:
                            Console.WriteLine("CanSetDisplayArea");
                            break;
                        case Capabilities.HasExternalSignal:
                            Console.WriteLine("HasExternalSignal");
                            break;
                        case Capabilities.HasEyeImages:
                            Console.WriteLine("HasEyeImages");
                            break;
                        case Capabilities.HasGazeData:
                            Console.WriteLine("HasGazeData");
                            break;
                        case Capabilities.HasHMDGazeData:
                            Console.WriteLine("HasHMDGazeData");
                            break;
                        case Capabilities.HasHMDLensConfig:
                            Console.WriteLine("HasHMDLensConfig");
                            break;
                        case Capabilities.None:
                            Console.WriteLine("None");
                            break;
                        default:
                            Console.WriteLine("Error in Device Capabilities");
                            break;
                    }
                    Console.WriteLine("Device Address: " + eyeTracker.Address);
                    Console.WriteLine("Device Name: " + eyeTracker.DeviceName);
                    Console.WriteLine("Firmware Version: " + eyeTracker.FirmwareVersion);
                    Console.WriteLine("Runtime Version: " + eyeTracker.RuntimeVersion);
                    Console.WriteLine("Serial Number: " + eyeTracker.SerialNumber);

                    eyeTracker.HMDGazeDataReceived += OnHMDDataReceived;
                }
                else
                {
                    Console.WriteLine("No eye tracker was detected.");
                }

                while (!cts.IsCancellationRequested)
                {
                    // Events handle Eye Data
                    Console.WriteLine("StartThread Loop");
                    Thread.Sleep(10);
                    if (gazeData != null)
                    {
                        Console.WriteLine("gazeData != null");
                    }
                }
            });
        }

        public void Teardown() => EyeTrackingOperations.Terminate();
    }
}