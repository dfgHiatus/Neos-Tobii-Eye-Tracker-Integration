using BaseX;

namespace Tobii.XR
{
    public abstract class EyeTrackingFilterBase
    {
        /// <summary>
        /// Applies filter to data parameter
        /// </summary>
        /// <param name="data">Eye tracking data that will be modified</param>
        /// <param name="forward">A unit direction vector pointing forward in the coordinate system used by the eye tracking data</param>
        public abstract void Filter(TobiiXR_EyeTrackingData data, float3 forward);
    }
}