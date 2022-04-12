using System;
using System.Collections.Generic;
using System.Threading;
using Tobii.StreamEngine;

namespace TobiiAPI
{
    public class TobiiScreen
    {
        public struct TobiiStruct
        {
            public static string deviceName;
            public static long Timestamp;
            public static bool IsDebug;

            // If the tracking should be used at all (here, Screen-mode vs VR)
            // These should always be true
            public static bool LeftIsDeviceActive;
            public static bool RightIsDeviceActive;
            public static bool CombinedIsDeviceActive;
            public static bool LeftIsTracking;
            public static bool RightIsTracking;
            public static bool CombinedIsTracking;

            // If the tracking is reliable enough to be used
            public static bool LeftIsDeviceTracking;
            public static bool RightIsDeviceTracking;
            public static bool CombinedIsDeviceTracking;

            public static Tuple<float, float, float> LeftEyeDirection;
            public static Tuple<float, float, float> RightEyeDirection;
            public static Tuple<float, float, float> CombinedEyeDirection;
            public static Tuple<float, float, float> LeftEyeRawPosition;
            public static Tuple<float, float, float> RightEyeRawPosition;
            public static Tuple<float, float, float> CombinedEyeRawPosition;

            // Normalized 0 to 1
            public static float LeftSqueeze;
            public static float RightSqueeze;
            public static float CombinedSqueeze;

            public static float LeftWiden;
            public static float RightWiden;
            public static float CombinedWiden;

            public static float LeftFrown;
            public static float RightFrown;
            public static float CombinedFrown;

            public static float LeftOpenness;
            public static float RightOpenness;
            public static float CombinedOpenness;

            // PupilDiameter is in mm
            public static float LeftPupilDiameter;
            public static float RightPupilDiameter;
            public static float CombinedPupilDiameter;

            public static IntPtr deviceContext;
            public static IntPtr apiContext;
            public static CancellationTokenSource _cancellationToken;
            public static tobii_error_t Error_T;
            public static Thread _worker;

        }
        public static bool Init()
        {
            // Create API context
            TobiiStruct.Error_T = Interop.tobii_api_create(out TobiiStruct.apiContext, null);
            if(!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Could not create API Content for the Tobii Stream Engine");
                return false;
            }

            // Enumerate devices to find connected eye trackers
            List<string> urls;
            TobiiStruct.Error_T = Interop.tobii_enumerate_local_device_urls(TobiiStruct.apiContext, out urls);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR) || urls.Count == 0)
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Connect to the first tracker found
            TobiiStruct.Error_T = Interop.tobii_device_create(TobiiStruct.apiContext, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out TobiiStruct.deviceContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            tobii_device_info_t info;
            Interop.tobii_get_device_name(TobiiStruct.deviceContext, out TobiiStruct.deviceName);
            Interop.tobii_get_device_info(TobiiStruct.deviceContext, out info);

            // Debugging: See what capabilties the device has
            bool supports = false;
            foreach (tobii_capability_t capability in Enum.GetValues(typeof(tobii_capability_t)))
            {
                TobiiStruct.Error_T = Interop.tobii_capability_supported(TobiiStruct.deviceContext, capability, out supports);
                if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine($"Well, something happened: {TobiiStruct.Error_T}");
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
                TobiiStruct.Error_T = Interop.tobii_stream_supported(TobiiStruct.deviceContext, stream, out supports);
                if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
                {
                    Console.WriteLine($"Well, something happened: {TobiiStruct.Error_T}");
                    continue;
                }
                if (supports)
                {
                    Console.WriteLine($"Device supports stream: {stream}.");
                }
            }

            // Subscribe to raw eye position data
            TobiiStruct.Error_T = Interop.tobii_gaze_origin_subscribe(TobiiStruct.deviceContext, OnRawPosition);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Subscribe to raw eye position data
            TobiiStruct.Error_T = Interop.tobii_gaze_point_subscribe(TobiiStruct.deviceContext, OnDirection);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Subscribe to user precense data
            TobiiStruct.Error_T = Interop.tobii_user_presence_subscribe(TobiiStruct.deviceContext, OnUserPrecense);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Error: No device found");
                return false;
            }

            // Console.WriteLine($"Connected to {TobiiStruct.deviceName} with firmware version {info.firmware_version} and runtime build version {info.runtime_build_version}.");
            // GetUpdateThreadFunc();
            // _worker.Start();
            return true;
        }

        /// <summary>
        /// Tobii callback that provides normalized raw position x, y, z relative to the center of the screen.
        /// </summary>
        private static void OnRawPosition(ref tobii_gaze_origin_t gaze_origin, IntPtr user_data)
        {
            if (gaze_origin.left_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                TobiiStruct.LeftEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.left.x,
                    gaze_origin.left.y,
                    gaze_origin.left.z);
            }

            if (gaze_origin.right_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                TobiiStruct.RightEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.right.x,
                    gaze_origin.right.y,
                    gaze_origin.right.z);
            }

            if (gaze_origin.left_validity == tobii_validity_t.TOBII_VALIDITY_VALID
            && gaze_origin.right_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                TobiiStruct.CombinedEyeRawPosition = new Tuple<float, float, float>(
                    gaze_origin.left.x + gaze_origin.right.x / 2,
                    gaze_origin.left.y + gaze_origin.right.y / 2,
                    gaze_origin.left.z + gaze_origin.right.z / 2);
            }
        }
        /// <summary>
        /// Tobii callback that provides IsDeviceActive 
        /// </summary>
        private static void OnUserPrecense(tobii_user_presence_status_t status, long timestamp_us, IntPtr user_data)
        {
            // Check that the data is valid before using it
            if (status == tobii_user_presence_status_t.TOBII_USER_PRESENCE_STATUS_PRESENT)
            {
                TobiiStruct.LeftIsDeviceTracking = true;
                TobiiStruct.RightIsDeviceTracking = true;
                TobiiStruct.CombinedIsDeviceTracking = true;
            }
            else if (status == tobii_user_presence_status_t.TOBII_USER_PRESENCE_STATUS_AWAY)
            {
                TobiiStruct.LeftIsDeviceTracking = false;
                TobiiStruct.RightIsDeviceTracking = false;
                TobiiStruct.CombinedIsDeviceTracking = false;
            }
        }
        /// <summary>
        /// Tobii callback that provides normalized eye direction through some funny math.
        /// IE a projection from a unit plane tangent to a unit sphere. Thanks Pimax Mod!
        /// </summary>
        private static void OnDirection(ref tobii_gaze_point_t gaze_point, IntPtr user_data)
        {
            if(gaze_point.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
            {
                // As x and y can be greater than 1, IE if the user is looking off screen
                var dir = new Tuple<float, float, float>(
                    (float) Math.Tan(Math.Clamp(gaze_point.position.x, 0, 1)),
                    (float) Math.Tan(Math.Clamp(gaze_point.position.y, 0, 1)),
                    1f);

                TobiiStruct.LeftEyeDirection = dir;
                TobiiStruct.RightEyeDirection = dir;
                TobiiStruct.CombinedEyeDirection = dir;
            }
        }
        public static void Update()
        {
            // Optionally block this thread until data is available. Especially useful if running in a separate thread.
            Interop.tobii_wait_for_callbacks(new[] { TobiiStruct.deviceContext });
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR || !(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_TIMED_OUT)))
            {
                Console.WriteLine("Something bad happened while updating Tobii Eye Callbacks.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            // Process callbacks on this thread if data is available
            Interop.tobii_device_process_callbacks(TobiiStruct.deviceContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while processing Tobii Eye Callbacks.");
                Console.WriteLine(TobiiStruct.Error_T);
            }
        }
        public static void Teardown()
        {
            TobiiStruct._cancellationToken.Cancel();

            TobiiStruct.Error_T = Interop.tobii_gaze_point_unsubscribe(TobiiStruct.deviceContext);
            if(!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze point.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            TobiiStruct.Error_T = Interop.tobii_user_position_guide_unsubscribe(TobiiStruct.deviceContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down gaze origin.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            TobiiStruct.Error_T = Interop.tobii_user_presence_unsubscribe(TobiiStruct.deviceContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down user presence.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            TobiiStruct.Error_T = Interop.tobii_device_destroy(TobiiStruct.deviceContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            TobiiStruct.Error_T = Interop.tobii_api_destroy(TobiiStruct.apiContext);
            if (!(TobiiStruct.Error_T == tobii_error_t.TOBII_ERROR_NO_ERROR))
            {
                Console.WriteLine("Something bad happened while shutting down.");
                Console.WriteLine(TobiiStruct.Error_T);
            }

            TobiiStruct._cancellationToken.Dispose();
        }
    }
}
