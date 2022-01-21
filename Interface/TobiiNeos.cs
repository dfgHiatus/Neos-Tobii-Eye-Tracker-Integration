using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;
using Tobii.Research;
using System.ComponentModel;

namespace Neos_Tobii_Eye_Integration
{
    public class TobiiNeos : IEyeTracker
    {
        public Uri Address => throw new NotImplementedException();

        public string DeviceName => "Vive Eye Devkit";

        public string SerialNumber => "N/A";

        public string Model => "Vive Eye Devkit";

        public string FirmwareVersion => "1.0.0";

        public string RuntimeVersion => "1.0.0";

        public Capabilities DeviceCapabilities => throw new NotImplementedException();

        public event EventHandler<GazeDataEventArgs> GazeDataReceived;
        public event EventHandler<UserPositionGuideEventArgs> UserPositionGuideReceived;
        public event EventHandler<HMDGazeDataEventArgs> HMDGazeDataReceived;
        public event EventHandler<TimeSynchronizationReferenceEventArgs> TimeSynchronizationReferenceReceived;
        public event EventHandler<ExternalSignalValueEventArgs> ExternalSignalReceived;
        public event EventHandler<EventErrorEventArgs> EventErrorOccurred;
        public event EventHandler<EyeImageEventArgs> EyeImageReceived;
        public event EventHandler<EyeImageRawEventArgs> EyeImageRawReceived;
        public event EventHandler<GazeOutputFrequencyEventArgs> GazeOutputFrequencyChanged;
        public event EventHandler<CalibrationModeEnteredEventArgs> CalibrationModeEntered;
        public event EventHandler<CalibrationModeLeftEventArgs> CalibrationModeLeft;
        public event EventHandler<CalibrationChangedEventArgs> CalibrationChanged;
        public event EventHandler<DisplayAreaEventArgs> DisplayAreaChanged;
        public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        public event EventHandler<ConnectionRestoredEventArgs> ConnectionRestored;
        public event EventHandler<TrackBoxEventArgs> TrackBoxChanged;
        public event EventHandler<EyeTrackingModeChangedEventArgs> EyeTrackingModeChanged;
        public event EventHandler<DeviceFaultsEventArgs> DeviceFaults;
        public event EventHandler<DeviceWarningsEventArgs> DeviceWarnings;
        public event PropertyChangedEventHandler PropertyChanged;

        public void ApplyCalibrationData(CalibrationData calibrationData)
        {
            throw new NotImplementedException();
        }

        public void ClearAppliedLicenses()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public EyeTrackingModeCollection GetAllEyeTrackingModes()
        {
            throw new NotImplementedException();
        }

        public GazeOutputFrequencyCollection GetAllGazeOutputFrequencies()
        {
            throw new NotImplementedException();
        }

        public DisplayArea GetDisplayArea()
        {
            throw new NotImplementedException();
        }

        public string GetEyeTrackingMode()
        {
            throw new NotImplementedException();
        }

        public float GetGazeOutputFrequency()
        {
            throw new NotImplementedException();
        }

        public HMDLensConfiguration GetHMDLensConfiguration()
        {
            throw new NotImplementedException();
        }

        public TrackBox GetTrackBox()
        {
            throw new NotImplementedException();
        }

        public CalibrationData RetrieveCalibrationData()
        {
            throw new NotImplementedException();
        }

        public void SetDeviceName(string deviceName)
        {
            throw new NotImplementedException();
        }

        public void SetDisplayArea(DisplayArea displayArea)
        {
            throw new NotImplementedException();
        }

        public void SetEyeTrackingMode(string eyeTrackingMode)
        {
            throw new NotImplementedException();
        }

        public void SetGazeOutputFrequency(float gazeOutputFrequency)
        {
            throw new NotImplementedException();
        }

        public void SetHMDLensConfiguration(HMDLensConfiguration hmdLensConfiguration)
        {
            throw new NotImplementedException();
        }

        public bool TryApplyLicenses(LicenseCollection licenses, out FailedLicenseCollection failedLicenses)
        {
            throw new NotImplementedException();
        }
    }
}
