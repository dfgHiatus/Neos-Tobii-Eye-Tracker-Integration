using BaseX;
using NeosModLoader;
using Tobii.Research;
using static Neos_Tobii_Eye_Integration.Neos_Tobii_Eye;

namespace Neos_Tobii_Eye_Integration.Helpers
{
	public class TobiiVR : NeosMod
	{
		// Problem?
		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-1.0.3";

		public static void EyeTracker_HMDGazeDataReceived(object sender, HMDGazeDataEventArgs e)
		{
			// TODO Check if both are valid post debugging

			leftIsValid = e.LeftEye.GazeOrigin.Validity == Validity.Valid;
			leftBlink = e.LeftEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			leftRawPupil = e.LeftEye.Pupil.PupilDiameter;

			if (e.LeftEye.GazeOrigin.Validity == Validity.Valid && e.LeftEye.GazeDirection.Validity == Validity.Valid)
			{
				leftRawPos = new float3(
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.X,
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.Y,
					e.LeftEye.GazeOrigin.PositionInHMDCoordinates.Z);
				leftRawRot = new float3(
					e.LeftEye.GazeDirection.UnitVector.X,
					e.LeftEye.GazeDirection.UnitVector.Y,
					e.LeftEye.GazeDirection.UnitVector.Z);
			}

			rightIsValid = e.RightEye.GazeOrigin.Validity == Validity.Valid;
			rightBlink = e.RightEye.GazeOrigin.Validity == Validity.Valid ? 1f : 0f;
			rightRawPupil = e.RightEye.Pupil.PupilDiameter;

			if (e.RightEye.GazeOrigin.Validity == Validity.Valid && e.RightEye.GazeDirection.Validity == Validity.Valid)
			{
				rightRawPos = new float3(
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.X,
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.Y,
					e.RightEye.GazeOrigin.PositionInHMDCoordinates.Z);
				rightRawRot = new float3(
					e.RightEye.GazeDirection.UnitVector.X,
					e.RightEye.GazeDirection.UnitVector.Y,
					e.RightEye.GazeDirection.UnitVector.Z);
			}

			timestamp = e.DeviceTimeStamp;
		}
	}	
}
