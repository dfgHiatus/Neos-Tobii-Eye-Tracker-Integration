using FrooxEngine;
using HarmonyLib;
using NeosModLoader;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-vr-1.0.7";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

        private static TobiiInputDevice tobiiInputDevice;

        public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration");
			harmony.PatchAll();

            tobiiInputDevice.Teardown();
            Engine.Current.OnShutdown += () => tobiiInputDevice.Teardown();
        }
	}
}