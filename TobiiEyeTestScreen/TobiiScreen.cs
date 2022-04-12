using System;
using System.Collections.Generic;
using System.Threading;
using Tobii.StreamEngine;

namespace TobiiAPI
{
    public class TobiiScreen
    {
        public string deviceName;
        public long Timestamp;
        public bool IsDebug;

        // If the tracking should be used at all (here, Screen-mode vs VR)
        // These should always be true
        public bool LeftIsDeviceActive;
        public bool RightIsDeviceActive;
        public bool CombinedIsDeviceActive;
        public bool LeftIsTracking;
        public bool RightIsTracking;
        public bool CombinedIsTracking;

        // If the tracking is reliable enough to be used
        public bool LeftIsDeviceTracking;
        public bool RightIsDeviceTracking;
        public bool CombinedIsDeviceTracking;

        public Tuple<float, float, float> LeftEyeDirection;
        public Tuple<float, float, float> RightEyeDirection;
        public Tuple<float, float, float> CombinedEyeDirection;
        public Tuple<float, float, float> LeftEyeRawPosition;
        public Tuple<float, float, float> RightEyeRawPosition;
        public Tuple<float, float, float> CombinedEyeRawPosition;

        // Normalized 0 to 1
        public float LeftSqueeze;
        public float RightSqueeze;
        public float CombinedSqueeze;

        public float LeftWiden;
        public float RightWiden;
        public float CombinedWiden;

        public float LeftFrown;
        public float RightFrown;
        public float CombinedFrown;

        public float LeftOpenness;
        public float RightOpenness;
        public float CombinedOpenness;

        // PupilDiameter is in mm
        public float LeftPupilDiameter;
        public float RightPupilDiameter;
        public float CombinedPupilDiameter;

        private IntPtr deviceContext;
        private IntPtr apiContext;
        private CancellationTokenSource _cancellationToken;
        private tobii_error_t Error_T;
        private Thread _worker;
        public TobiiScreen() 
        {
            LeftIsDeviceActive = true;
            RightIsDeviceActive = true;
            CombinedIsDeviceActive = true;
            LeftIsTracking = true;
            RightIsTracking = true;
            CombinedIsTracking = true;
            LeftIsDeviceTracking = true;
            RightIsDeviceTracking = true;
            CombinedIsDeviceTracking = true;
            IsDebug = false;

            LeftSqueeze = 0f;
            RightSqueeze = 0f;
            CombinedSqueeze = 0f;

            LeftWiden = 0f;
            RightWiden = 0f;
            CombinedWiden = 0f;

            LeftFrown = 0f;
            RightFrown = 0f;
            CombinedFrown = 0f;

            LeftOpenness = 1f;
            RightOpenness = 1f;
            CombinedOpenness = 1f;

            LeftPupilDiameter = 0.0035f;
            RightPupilDiameter = 0.0035f;
            CombinedPupilDiameter = 0.0035f;

            LeftEyeDirection = new Tuple<float, float, float>(0, 0, 1);
            RightEyeDirection = new Tuple<float, float, float>(0, 0, 1);
            CombinedEyeDirection = new Tuple<float, float, float>(0, 0, 1);
            LeftEyeRawPosition = new Tuple<float, float, float> (0,0,0);
            RightEyeRawPosition = new Tuple<float, float, float>(0, 0, 0);
            CombinedEyeRawPosition = new Tuple<float, float, float>(0, 0, 0);

            Timestamp = 0;
        }
        public bool Init()
        {
            // Create API context
            Error_T = Interop.tobii_api_create(out apiContext, null);
            if(!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Could not create API Content for the Tobii Stream Engine");
                return false;
            }

            // Enumerate devices to find connected eye trackers
            List<string> urls;
            Error_T = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR) || urls.Count == 0)
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Connect to the first tracker found
            Error_T = Interop.tobii_device_create(apiContext, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            tobii_device_info_t info;
            Interop.tobii_get_device_name(deviceContext, out deviceName);
            Interop.tobii_get_device_info(deviceContext, out info);

            // Debugging: See what capabilties the device has
            bool supports = false;
            foreach (tobii_capability_t capability in Enum.GetValues(typeof(tobii_capability_t)))
            {
                Error_T = Interop.tobii_capability_supported(deviceContext, capability, out supports);
                if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine($"Well, something happened: {Error_T}");
                    continue;
                }
                if (supports)
                {
                    Console.WriteLine($"Device supports capability: {capability}.");
                }
            }

            // Debugging: See what streams the device has
            supports = false;
            foreach (tobii_stream_t stream in Enum.GetValues(typeof(tobii_stream_t)))
            {
                Error_T = Interop.tobii_stream_supported(deviceContext, stream, out supports);
                if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine($"Well, something happened: {Error_T}");
                    continue;
                }
                if (supports)
                {
                    Console.WriteLine($"Device supports stream: {stream}.");
                }
            }

            // Subscribe to raw eye position data
            Error_T = Interop.tobii_gaze_origin_subscribe(deviceContext, OnRawPosition);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Subscribe to raw eye position data
            Error_T = Interop.tobii_gaze_point_subscribe(deviceContext, OnDirection);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Subscribe to user precense data
            Error_T = Interop.tobii_user_presence_subscribe(deviceContext, OnUserPrecense);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            Console.WriteLine($"Connected to {deviceName} with firmware version {info.firmware_version} and runtime build version {info.runtime_build_version}.");
            GetUpdateThreadFunc();
            if (IsDebug)
            {
                Console.WriteLine("Stopping here, but the rest of the program should work fine.");
                Thread.Sleep(2000);
                return false;
            }
            _worker.Start();
            return true;
        }

        /// <summary>
        /// Tobii callback that provides normalized raw position x, y, z relative to the center of the screen.
        /// </summary>
        private void OnRawPosition(ref tobii_gaze_origin_t gaze_origin, IntPtr user_data)
        {
            if (gaze_origin.left_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                LeftEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.left.x,
                    gaze_origin.left.y,
                    gaze_origin.left.z);
            }

            if (gaze_origin.right_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                RightEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.right.x,
                    gaze_origin.right.y,
                    gaze_origin.right.z);
            }

            if (gaze_origin.left_validity == tobii_validity_t.TOBII_VALIDITY_VALID
            && gaze_origin.right_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                CombinedEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.left.x + gaze_origin.right.x / 2,
                    gaze_origin.left.y + gaze_origin.right.y / 2,
                    gaze_origin.left.z + gaze_origin.right.z / 2);
            }
        }
        /// <summary>
        /// Tobii callback that provides IsDeviceActive 
        /// </summary>
        private void OnUserPrecense(tobii_user_presence_status_t status, long timestamp_us, IntPtr user_data)
        {
            // Check that the data is valid before using it
            if (status == tobii_user_presence_status_t.TOBII_USER_PRESENCE_STATUS_PRESENT)
            {
                LeftIsDeviceTracking = true;
                RightIsDeviceTracking = true;
                CombinedIsDeviceTracking = true;
            }
            else if (status == tobii_user_presence_status_t.TOBII_USER_PRESENCE_STATUS_AWAY)
            {
                LeftIsDeviceTracking = false;
                RightIsDeviceTracking = false;
                CombinedIsDeviceTracking = false;
            }
        }
        /// <summary>
        /// Tobii callback that provides normalized eye direction through some funny math.
        /// IE a projection from a unit plane tangent to a unit sphere. Thanks Pimax Mod!
        /// </summary>
        private void OnDirection(ref tobii_gaze_point_t gaze_point, IntPtr user_data)
        {
            if(gaze_point.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                // As x and y can be greater than 1, IE if the user is looking off screen
                var dir = new Tuple<float, float, float>(
                    (float) Math.Tan(Math.Clamp(gaze_point.position.x, 0, 1)),
                    (float) Math.Tan(Math.Clamp(gaze_point.position.y, 0, 1)),
                    1f);

                LeftEyeDirection = dir;
                RightEyeDirection = dir;
                CombinedEyeDirection = dir;
            }
        }
        private void GetUpdateThreadFunc()
        {
            _cancellationToken = new CancellationTokenSource();
            _worker = new Thread(() =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    _cancellationToken.Token.ThrowIfCancellationRequested();
                    Update();
                    if (IsDebug)
                    {
                        PrintDebugString();
                    }
                    Thread.Sleep(10);
                }
            });
        }
        public void Update()
        {
            // Optionally block this thread until data is available. Especially useful if running in a separate thread.
            Interop.tobii_wait_for_callbacks(new[] { deviceContext });
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR || !(Error_T == tobii_error_t.TOBII_ERROR_TIMED_OUT)))
            {
                Console.WriteLine("Something bad happened while updating Tobii Eye Callbacks.");
                Console.WriteLine(Error_T);
            }

            // Process callbacks on this thread if data is available
            Interop.tobii_device_process_callbacks(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while processing Tobii Eye Callbacks.");
                Console.WriteLine(Error_T);
            }

            // Get device timestamp. This is consistent across all callbacks!
            Error_T = Interop.tobii_system_clock(deviceContext, out Timestamp);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while updating Tobii Eye Timestamp.");
            }
        }
        public void Teardown()
        {
            _cancellationToken.Cancel();

            Error_T = Interop.tobii_gaze_point_unsubscribe(deviceContext);
            if(!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze point.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_user_position_guide_unsubscribe(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze origin.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_user_presence_unsubscribe(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down user presence.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_device_destroy(deviceContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(Error_T);
            }

            Error_T = Interop.tobii_api_destroy(apiContext);
            if (!(Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(Error_T);
            }

            _cancellationToken.Dispose();
        }
        public void PrintDebugString()
        {
            Console.WriteLine(
        $"Name {deviceName}, TS: {Timestamp}, LDA {LeftIsDeviceActive}, RDA {RightIsDeviceActive}, CDA {CombinedIsDeviceActive} \n" +
        $"LIS { LeftIsTracking}, RIS {RightIsTracking}, CIS {CombinedIsTracking} \n" +
        $"LIT { LeftIsDeviceTracking}, RIT {RightIsDeviceTracking}, CIT {CombinedIsDeviceTracking} \n" +
        $"LD { LeftEyeDirection}, RD {RightEyeDirection}, CD {CombinedEyeDirection} \n" +
        $"LRP { LeftEyeRawPosition}, RRP {RightEyeRawPosition}, CRP {CombinedEyeRawPosition} \n" +
        $"LS { LeftSqueeze}, RS {RightSqueeze}, CS {CombinedSqueeze} \n" +
        $"LW { LeftWiden}, RW {RightWiden}, CW {CombinedWiden} \n" +
        $"LF { LeftFrown}, RF {RightFrown}, CF {CombinedFrown} \n" +
        $"LO { LeftOpenness}, RO {RightOpenness}, CO {CombinedOpenness} \n" +
        $"LPD { LeftPupilDiameter}, RPD {RightPupilDiameter}, CPD {CombinedPupilDiameter}");
            Console.Out.Flush();
        }
    }
}
