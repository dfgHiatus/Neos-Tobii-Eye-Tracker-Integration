using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TobiiAPI;

namespace TobiiMemoryMap
{
    public class MemoryMap
    {
        public static void Main(string[] args)
        {

            if (!TobiiScreen.Init())
            {
                Console.WriteLine("Gaze tracking is not allowed! Please enable it in the Tobii App!");
                return;
            }
            else 
            {
                TobiiScreen.TobiiStruct gazeData = new TobiiScreen.TobiiStruct();

                using (var memMapFile = MemoryMappedFile.CreateNew("VarjoEyeTracking", Marshal.SizeOf(gazeData)))
                {
                    using (var accessor = memMapFile.CreateViewAccessor())
                    {
                        Console.WriteLine("Eye tracking session has started!");
                        while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter))
                        {
                            TobiiScreen.Update();
                            accessor.Write(0, ref gazeData);
                        }
                    }
                }
                TobiiScreen.Teardown();
            }  
        }
    }
}