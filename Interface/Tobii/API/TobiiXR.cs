// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved

namespace Tobii.XR
{
    using System.Collections.Generic;
    using System.Text;
    using Tobii.G2OM;
    using Tobii.XR.Internal;
    using FrooxEngine;
    using BaseX;

    /// <summary>
    /// Static access point for Tobii XR eye tracker data.
    /// </summary>
    public static class TobiiXR
    {
        private static readonly TobiiXRInternal _internal = new TobiiXRInternal();
        private static Slot _updaterGameObject;
        private static readonly TobiiXR_EyeTrackingData _eyeTrackingDataLocal = new TobiiXR_EyeTrackingData();
        private static readonly TobiiXR_EyeTrackingData _eyeTrackingDataWorld = new TobiiXR_EyeTrackingData();
        // private static TobiiXRAdvanced _advanced;

        /// <summary>
        /// Gets eye tracking data in the selected tracking space. Unless the underlying eye tracking
        /// provider does prediction, this data is not predicted.
        /// Subsequent calls within the same frame will return the same value.
        /// </summary>
        /// <param name="trackingSpace">The tracking space to report eye tracking data in.</param>
        /// <returns>The last (newest) <see cref="TobiiXR_EyeTrackingData"/>.</returns>
        public static TobiiXR_EyeTrackingData GetEyeTrackingData(TobiiXR_TrackingSpace trackingSpace)
        {
            switch (trackingSpace)
            {
                case TobiiXR_TrackingSpace.Local:
                    return _eyeTrackingDataLocal;
                case TobiiXR_TrackingSpace.World:
                    return _eyeTrackingDataWorld;
            }

            throw new System.Exception("Unknown tracking space: " + trackingSpace);
        }

        private static bool IsRunning
        {
            get { return Internal.Provider != null; }
        }
        
        public static bool Start(TobiiXR_Settings settings = null)
        {
            if (IsRunning) Stop();

            // Create default settings if none were provided
            if (settings == null)
            {
                settings = new TobiiXR_Settings();
            }
            Internal.Settings = settings;
            
            // Check if a license was supplied
            string licenseKey = null;
            if (settings.LicenseAsset != null) // Prioritize asset
            {
                UniLog.Log("Using license asset from settings");
                licenseKey = Encoding.Unicode.GetString(settings.LicenseAsset.bytes);
            }
            else if (!string.IsNullOrEmpty(settings.OcumenLicense)) // Second priority is license as text
            {
                UniLog.Log("Using license string from settings");
                licenseKey = settings.OcumenLicense;
            }
            
            // Setup eye tracking provider
            if (settings.AdvancedEnabled)
            {
                UniLog.Log("Advanced eye tracking enabled so TobiiXR will use Tobii provider for eye tracking.");
                if (string.IsNullOrEmpty(licenseKey))
                {
                    throw new System.Exception("An Ocumen license is required to use the advanced API. Read more about Ocumen here: https://vr.tobii.com/sdk/solutions/tobii-ocumen/");
                }

                var provider = new TobiiProvider();
                Internal.Provider = provider;
                var result = provider.InitializeWithLicense(licenseKey, true);
                // if (settings.PopupLicenseValidationErrors && provider.FriendlyValidationErrors.Count > 0) TobiiNotificationView.Show(provider.FriendlyValidationErrors[0]);
                if (!result)
                {
                    UniLog.Log("Failed to connect to a supported eye tracker. TobiiXR will NOT be available.");
                    return false;
                }

                if (provider.HasValidOcumenLicense)
                {
                    UniLog.Log("Ocumen license valid");
                    UniLog.Log("Contact DFG becuase he hasn't got this far yet");
                    // _advanced = new TobiiXRAdvanced(provider);
                }
                else
                {
                    UniLog.Log("Ocumen license INVALID. Advanced API will NOT be available.");
                }
            }
            else if (!string.IsNullOrEmpty(licenseKey)) // A license without feature group Professional was provided
            {
                UniLog.Log("An explicit license was provided so TobiiXR will use Tobii provider for eye tracking.");
                var provider = new TobiiProvider();
                Internal.Provider = provider;

                // Try to connect to an eye tracker
                if (provider.InitializeWithLicense(licenseKey, false))
                {
                    if (settings.PopupLicenseValidationErrors && provider.FriendlyValidationErrors.Count > 0)
                    {
                        UniLog.Log("Connected but license validation failed");
                    }
                }
                else // Failed to connect
                {
                    UniLog.Log("Failed to connect to a supported eye tracker. TobiiXR will NOT be available.");
                    return false;
                }
            }
            else
            {
                Internal.Provider = settings.EyeTrackingProvider;

                if (Internal.Provider == null)
                {
                    Internal.Provider = new NoseDirectionProvider();
                    UniLog.Log($"All configured providers failed. Using ({Internal.Provider.GetType().Name}) as fallback.");
                }

                UniLog.Log($"Starting TobiiXR with ({Internal.Provider}) as provider for eye tracking.");
            }
            
            // Setup G2OM
            if (settings.G2OM != null)
            {
                Internal.G2OM = settings.G2OM;
            }
            else
            {
                Internal.G2OM = G2OM.Create(new G2OM_Description
                {
                    LayerMask = settings.LayerMask,
                    HowLongToKeepCandidatesInSeconds = settings.HowLongToKeepCandidatesInSeconds
                });
            }

            // Create GameObject with TobiiXR_Lifecycle to give us Unity events
            _updaterGameObject = Userspace.UserspaceWorld.AddSlot("TobiiXR Updater");
            var updater = _updaterGameObject.AttachComponent<TobiiXR_Lifecycle>();
            updater.OnUpdateAction += Tick;
            updater.OnDisableAction += Internal.G2OM.Clear;
            updater.OnApplicationQuitAction += Stop;

            return true;
        }

        public static void Stop()
        {
            if (!IsRunning) return;

            Internal.G2OM.Destroy();
            Internal.Provider.Destroy();

            if (_updaterGameObject != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    Object.Destroy(_updaterGameObject.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_updaterGameObject.gameObject);
                }
#else
                _updaterGameObject.Destroy();
            // Object.Destroy(_updaterGameObject.gameObject);
#endif
            }


            _updaterGameObject = null;
            Internal.G2OM = null;
            Internal.Provider = null;
        }

        private static void Tick()
        {
            Internal.Provider.Tick();
            Internal.Provider.GetEyeTrackingDataLocal(_eyeTrackingDataLocal);
            EyeTrackingDataHelper.CopyAndTransformGazeData(_eyeTrackingDataLocal, _eyeTrackingDataWorld,
                Internal.Provider.LocalToWorldMatrix);

            // if (Internal.Filter != null && Internal.Filter.enabled)
            if (Internal.Filter != null)
            {
                var worldForward = Internal.Provider.LocalToWorldMatrix * float3.Forward;
                Internal.Filter.Filter(_eyeTrackingDataLocal, float3.Forward);
                Internal.Filter.Filter(_eyeTrackingDataWorld, worldForward);
            }

            // var g2omData = CreateG2OMData(_eyeTrackingDataWorld);
            // Internal.G2OM.Tick(g2omData);
        }

        // TODO CHECK
/*        private static G2OM_DeviceData CreateG2OMData(TobiiXR_EyeTrackingData data)
        {
            var t = Internal.Provider.LocalToWorldMatrix;
            return new G2OM_DeviceData
            {
                timestamp = data.Timestamp,
                gaze_ray_world_space = new G2OM_GazeRay
                {
                    is_valid = data.GazeRay.IsValid.ToByte(),
                    ray = G2OM_UnityExtensionMethods.CreateRay(data.GazeRay.Origin, data.GazeRay.Direction),
                },
                camera_up_direction_world_space = t.MultiplyVector(float3.up).AsG2OMfloat3(),
                camera_right_direction_world_space = t.MultiplyVector(float3.right).AsG2OMfloat3()
            };
        }*/

        /// <summary>
        /// For advanced and internal use only. Do not access this field before TobiiXR.Start has been called.
        /// Do not save a reference to the fields exposed by this class since TobiiXR will recreate them when restarted
        /// </summary>
        public static TobiiXRInternal Internal
        {
            get { return _internal; }
        }

        public class TobiiXRInternal
        {
            public TobiiXR_Settings Settings { get; internal set; }

            public IEyeTrackingProvider Provider { get; set; }

            public G2OM G2OM { get; internal set; }

            /// <summary>
            /// Defaults to no filter. If set, both EyeTrackingData and FocusedObjects will apply this filter to gaze data before using it
            /// </summary>
            public EyeTrackingFilterBase Filter
            {
                // get { return Settings == null ? null : Settings.EyeTrackingFilter; }
                get { return null; }
            }
        }
    }
}
