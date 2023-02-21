using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using Qromodyn;

namespace NeosTobiiEyeIntegration
{
	public class NeosTobiiEye : NeosMod
	{
		public override string Name => "Neos-Tobii-Eye-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "alpha-vr-1.0.7";
		public override string Link => "https://github.com/dfgHiatus/Neos-Tobii-Eye-Tracker-Integration";

        public override void OnEngineInit()
		{
            new Harmony("net.dfgHiatus.Neos-Tobii-Eye-Integration").PatchAll();

            TobiiInputDevice tobiiInputDevice = new TobiiInputDevice();
            tobiiInputDevice.Start();
            Engine.Current.OnShutdown += () => tobiiInputDevice.Stop();
        }
	}
}