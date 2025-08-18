using STDriver;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
namespace STConsole
{
    internal class Program
    {
        public static void Main()
        {
            STServoController Controller = new STServoController("COM5");
            ProtocolPacketHandler handler = new ProtocolPacketHandler(Controller);

            byte stsId = 1;
            List<int> speed = new List<int>() 
            {
                7000,
                0,
                -7000,
                0
            };
            byte acc = 255;


            // PING
            (int modelNumber, int result, int error) = handler.Ping(stsId);
            Console.WriteLine($"Model Number: {modelNumber}, Result: {result}, Error: {error}");

            // ROTATE
            handler.WheelMode(1);
            handler.WheelMode(2);
            foreach (int v in speed)
            {
                handler.WriteSpec(1, v, acc);
                handler.WriteSpec(2, -v, acc);
                Thread.Sleep(2500);
            }

            handler.WriteSpec(1, 7000, acc);
            Thread.Sleep(1000);

            handler.WriteSpec(2, 7000, acc);
            Thread.Sleep(1000);

            handler.WriteSpec(1, 0, acc);
            Thread.Sleep(1000);

            handler.WriteSpec(2, 0, acc);

            Controller.Close();
            Console.WriteLine("Port is Closed");
        }
    }
}