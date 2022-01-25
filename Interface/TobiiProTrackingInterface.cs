using System.Threading;
using Tobii.Research;
using BaseX;

namespace NeosTobiiEyeIntegration
{
    public class TobiiProTrackingInterface : NeosTobiiEye
    {
        private Thread _worker = null;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public IEyeTracker eyeTracker = null;
        
        public bool SupportsEye => true;
        public bool SupportsLip => false;

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
                if(_worker.IsAlive)
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
                    gazeDataEventArgs.LeftEye.GazeOrigin.PositionInUserCoordinates.Z); ;
            leftRawRot = new float3(
                    gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.X,
                    gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.Y,
                    gazeDataEventArgs.LeftEye.GazePoint.PositionInUserCoordinates.Z); ;
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
                    gazeDataEventArgs.LeftEye.GazeOrigin.PositionInHMDCoordinates.Z); ;
            leftRawRot = new float3(
                    gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.X,
                    gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.Y,
                    gazeDataEventArgs.LeftEye.GazeDirection.UnitVector.Z); ;
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
        }

        public void Teardown() => EyeTrackingOperations.Terminate();
        // Empty as it's not needed
        public void Update(){}
    }
}