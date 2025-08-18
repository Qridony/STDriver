using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace STDriver
{
    public class STServoController
    {
        private SerialPort _controller;
        public STServoController(string ComPort)
        {
            _controller = new SerialPort(ComPort);
            _controller.BaudRate = 1000000;
            //_controller.Parity = Parity.Odd;
            //_controller.StopBits = StopBits.Two;

            _controller.WriteTimeout = 10000;
            _controller.ReadTimeout = 10000;

            _controller.Open();
            _controller.DiscardInBuffer();
        }

        public byte[] ReadPort(int bytesToRead)
        {
            while (_controller.BytesToRead < bytesToRead)
            {
                Thread.Sleep(1);
            }

            byte[] rxPacket = new byte[bytesToRead];
            int bytesRead = _controller.Read(rxPacket, 0, bytesToRead);
            return rxPacket;
        }

        public void WritePort(byte[] Packet)
        {
            _controller.Write(Packet, 0, Packet.Length);
        }

        public void ClearPort()
        {
            _controller.DiscardOutBuffer();
        }

        public void Close()
        {
            _controller.Close();
        }
    }
}
