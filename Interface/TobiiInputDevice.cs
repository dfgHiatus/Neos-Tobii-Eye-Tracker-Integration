using BaseX;
using FrooxEngine;
using Qromodyn;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Tobii.StreamEngine;

namespace NeosTobiiEyeIntegration
{
    public class TobiiInputDevice : IInputDriver
    {
        private Eyes eyes;
        private const float DefaultPupilSize = 0.0035f;
        public int UpdateOrder => 100;

        // Initialise the variables needed for Tobii Stream Engine
        private IntPtr apiContext = Marshal.AllocHGlobal(1024);
        private IntPtr deviceContext = Marshal.AllocHGlobal(1024);
        private List<string> urls;

        private Thread _thread;
        private CancellationTokenSource _cancellationToken;

        // Initialise struct which tracking data will be inputted into
        private TobiiXRExternalTrackingDataStruct ParsedtrackingData;

        public void CollectDeviceInfos(DataTreeList list)
        {
            DataTreeDictionary dataTreeDictionary = new DataTreeDictionary();
            dataTreeDictionary.Add("Name", "Tobii Eye Tracking");
            dataTreeDictionary.Add("Type", "Eye Tracking");
            dataTreeDictionary.Add("Model", "Vive Eye Devkit");
            list.Add(dataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            eyes = new Eyes(inputInterface, "Tobii Vive Eye Tracking");
        }

        public void UpdateInputs(float deltaTime)
        {
            var status = true; // Engine.Current.InputInterface.VR_Active;
            eyes.IsEyeTrackingActive = status;

            UpdateEye(Project2DTo3D(ParsedtrackingData.left_eye.eye_x, ParsedtrackingData.left_eye.eye_y), 
                status, 
                ParsedtrackingData.left_eye.eye_lid_openness, 
                deltaTime, 
                eyes.LeftEye);

            UpdateEye(Project2DTo3D(ParsedtrackingData.right_eye.eye_x, ParsedtrackingData.right_eye.eye_y),
                status,
                ParsedtrackingData.right_eye.eye_lid_openness,
                deltaTime,
                eyes.RightEye);
            
            eyes.ComputeCombinedEyeParameters();
            eyes.FinishUpdate();
        }

        public void Start()
        {
            // Extract Embedded Tobii Stream Engine DLL for use in Tobii.StreamEngine.Native.cs
            // EmbeddedDllClass.ExtractEmbeddedDlls("tobii_stream_engine.dll",  TobiiEye.Properties.Resources.tobii_stream_engine);

            // Create API context
            var error = Native.tobii_api_create(out apiContext, null);
            UniLog.Log(error.ToString());
            UniLog.Log(apiContext);

            // Enumerate devices to find connected eye trackers
            error = Native.tobii_enumerate_local_device_urls(apiContext, out urls);
            if (urls.Count == 0)
            {
                UniLog.Error("Error: No device found");
                return;
            }

            UniLog.Log(error.ToString());
            UniLog.Log(urls.Count);
            UniLog.Log(urls[0]);

            // Connect to the first tracker found
            Native.tobii_device_create(
                apiContext, 
                urls[0], 
                Native.tobii_field_of_use_t.TOBII_FIELD_OF_USE_STORE_OR_TRANSFER_FALSE, 
                out deviceContext);
            
            _thread.Start(new ThreadStart(OuterLoop));
        }
        private void OuterLoop()
        {
            Native.tobii_wearable_consumer_data_subscribe(deviceContext, UpdateTobiiCallbacks);

            while (!_cancellationToken.IsCancellationRequested)
            {
                // Optionally block this thread until data is available. Especially useful if running in a separate thread.
                Native.tobii_wait_for_callbacks(new[] { deviceContext });

                // Process callbacks on this thread if data is available
                Native.tobii_device_process_callbacks(deviceContext);

                Thread.Sleep(10);
            }
        }

        private void UpdateTobiiCallbacks(ref tobii_wearable_consumer_data_t consumerData, IntPtr userData)
        {
            if (consumerData.left.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
                ParsedtrackingData.left_eye.eye_lid_openness = consumerData.left.blink 
                    == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f;

            if (consumerData.right.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
                ParsedtrackingData.right_eye.eye_lid_openness = consumerData.right.blink 
                    == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f;

            if (consumerData.left.pupil_position_in_sensor_area_validity 
                == tobii_validity_t.TOBII_VALIDITY_VALID && 
                consumerData.left.blink_validity 
                == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                ParsedtrackingData.left_eye.eye_x = consumerData.left.pupil_position_in_sensor_area_xy.x;
                ParsedtrackingData.left_eye.eye_y = consumerData.left.pupil_position_in_sensor_area_xy.y;
            }

            if (consumerData.right.pupil_position_in_sensor_area_validity 
                == tobii_validity_t.TOBII_VALIDITY_VALID && 
                consumerData.right.blink_validity 
                == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                ParsedtrackingData.right_eye.eye_x = consumerData.right.pupil_position_in_sensor_area_xy.x;
                ParsedtrackingData.right_eye.eye_y = consumerData.right.pupil_position_in_sensor_area_xy.y;
            }
        }
        
        private void UpdateEye(float3 gazeDirection, bool status, float openness, float deltaTime, Eye eye)
        {
            eye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            eye.IsTracking = status;

            if (eye.IsTracking)
            {
                eye.UpdateWithDirection(gazeDirection);
                eye.RawPosition = float3.Zero;
                eye.PupilDiameter = DefaultPupilSize;
            }

            eye.Openness = openness;
            eye.Widen = 0f;
            eye.Squeeze = 0f;
            eye.Frown = 0f;
        }

        private static float3 Project2DTo3D(float x, float y)
        {
            return new float3(MathX.Tan(x),
                              MathX.Tan(y),
                              1f).Normalized;
        }

        public void Stop()
        {
            _cancellationToken.Cancel();
            Native.tobii_wearable_consumer_data_unsubscribe(deviceContext);
            Native.tobii_device_destroy(deviceContext);
            Native.tobii_api_destroy(apiContext);
            Marshal.FreeHGlobal(deviceContext);
            Marshal.FreeHGlobal(apiContext);
            _thread.Abort();
        }

        // TobiiXR "single-eye" data response.
        public struct TobiiXRExternalTrackingDataEye
        {
            public float eye_lid_openness;
            public float eye_x;
            public float eye_y;
        }

        // TobiiXR "full-data" response from the external tracking system.
        public struct TobiiXRExternalTrackingDataStruct
        {
            public TobiiXRExternalTrackingDataEye left_eye;
            public TobiiXRExternalTrackingDataEye right_eye;
        }
    }
}
