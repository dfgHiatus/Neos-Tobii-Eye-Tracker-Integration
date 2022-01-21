//Copyright © 2018 – Property of Tobii AB(publ) - All Rights Reserved

using BaseX;

namespace Tobii.XR
{
    public static class EyeTrackingDataHelper
    {
        public static void Copy(TobiiXR_EyeTrackingData src, TobiiXR_EyeTrackingData dest)
        {
            dest.Timestamp = src.Timestamp;
            dest.GazeRay = src.GazeRay;
            dest.ConvergenceDistance = src.ConvergenceDistance;
            dest.ConvergenceDistanceIsValid = src.ConvergenceDistanceIsValid;
            dest.IsLeftEyeBlinking = src.IsLeftEyeBlinking;
            dest.IsRightEyeBlinking = src.IsRightEyeBlinking;
        }

        public static TobiiXR_EyeTrackingData Clone(TobiiXR_EyeTrackingData data)
        {
            var result = new TobiiXR_EyeTrackingData();
            Copy(data, result);
            return result;
        }

        public static void CopyAndTransformGazeData(TobiiXR_EyeTrackingData src, TobiiXR_EyeTrackingData dest, float4x4 transformMatrix)
        {
            Copy(src, dest);
            if (src.GazeRay.IsValid)
            {
                dest.GazeRay.Origin = transformMatrix.MultiplyPoint(src.GazeRay.Origin);
                dest.GazeRay.Direction = transformMatrix.MultiplyVector(src.GazeRay.Direction);
            }
        }

        public static void TransformGazeData(TobiiXR_EyeTrackingData data, float4x4 transformMatrix)
        {
            data.GazeRay.Origin = transformMatrix.MultiplyPoint(data.GazeRay.Origin);
            data.GazeRay.Direction = transformMatrix.MultiplyVector(data.GazeRay.Direction);
        }
    }
}
