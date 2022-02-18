using System;

namespace TobiiEyeTestScreen
{
    class Program
    {
        static void Main(string[] args)
        {
            TobiiScreen tobiiScreen = new TobiiScreen();
            tobiiScreen.Start();
        }
    }
}
