using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDriver
{
    public class ProtocolPacketHandler
    {
        private const int TXPACKET_MAX_LEN = 250;
        private const int RXPACKET_MAX_LEN = 250;

        // for Protocol Packet
        private const int PKT_HEADER0 = 0;
        private const int PKT_HEADER1 = 1;
        private const int PKT_ID = 2;
        private const int PKT_LENGTH = 3;
        private const int PKT_INSTRUCTION = 4;
        private const int PKT_ERROR = 4;
        private const int PKT_PARAMETER0 = 5;

        // Protocol Error bit
        private const int ERRBIT_VOLTAGE = 1;
        private const int ERRBIT_ANGLE = 2;
        private const int ERRBIT_OVERHEAT = 4;
        private const int ERRBIT_OVERELE = 8;
        private const int ERRBIT_OVERLOAD = 32;

        private STServoController _controller;

        public ProtocolPacketHandler(STServoController Conntroller)
        {
            _controller = Conntroller;
        }

        public int StsToHost(int a, int b)
        {
            if ((a & (1 << b)) != 0)
                return -(a & ~(1 << b));
            else
                return a;
        }

        public int StsToScs(int a, int b)
        {
            if (a < 0)
                return (-a | (1 << b));
            else
                return a;
        }

        public int StsMakeWord(int a, int b)
        {
            return (a & 0xFF) | ((b & 0xFF) << 8);
        }

        public int StsMakeDWord(int a, int b)
        {
            return (a & 0xFFFF) | ((b & 0xFFFF) << 16);
        }

        public int StsLoWord(int l)
        {
            return l & 0xFFFF;
        }

        public int StsHiWord(int h)
        {
            return (h >> 16) & 0xFFFF;
        }

        public int StsLoByte(int w)
        {
            return w & 0xFF;
        }

        public int StsHiByte(int w)
        {
            return (w >> 8) & 0xFF;
        }

        public string getTxRxResult(int result)
        {
            switch (result)
            {
                case Constans.COMM_SUCCESS: return "[TxRxResult] Communication success!";
                case Constans.COMM_PORT_BUSY: return "[TxRxResult] Port is in use!";
                case Constans.COMM_TX_FAIL: return "[TxRxResult] Failed transmit instruction packet!";
                case Constans.COMM_RX_FAIL: return "[TxRxResult] Failed get status packet from device!";
                case Constans.COMM_TX_ERROR: return "[TxRxResult] Incorrect instruction packet!";
                case Constans.COMM_RX_WAITING: return "[TxRxResult] Now receiving status packet!";
                case Constans.COMM_RX_TIMEOUT: return "[TxRxResult] There is no status packet!";
                case Constans.COMM_RX_CORRUPT: return "[TxRxResult] Incorrect status packet!";
                case Constans.COMM_NOT_AVAILABLE: return "[TxRxResult] Protocol does not support this function!";
                default: return "";
            }
        }

        public int TxPacket(byte[] txpacket)
        {
            byte checksum = 0;
            int total_packet_length = txpacket[PKT_LENGTH] + 4;  // 4: HEADER0 HEADER1 ID LENGTH

            // Optional: Check if port is busy
            //if (_controller.IsUsing)
            //    return Constans.COMM_PORT_BUSY;
            //_controller.IsUsing = true;

            if (total_packet_length > TXPACKET_MAX_LEN)
            {
                //_controller.IsUsing = false;
                return Constans.COMM_TX_ERROR;
            }

            txpacket[PKT_HEADER0] = 0xFF;
            txpacket[PKT_HEADER1] = 0xFF;

            for (int idx = 2; idx < total_packet_length - 1; idx++) // exclude checksum byte
            {
                checksum += txpacket[idx];
            }

            txpacket[total_packet_length - 1] = (byte)(~checksum & 0xFF);

            _controller.ClearPort();
            _controller.WritePort(txpacket);
            //int written_packet_length = _controller.WritePort(txpacket);

            //if (total_packet_length != written_packet_length)
            //{
            //    _controller.IsUsing = false;
            //    return Constans.COMM_TX_FAIL;
            //}

            return Constans.COMM_SUCCESS;
        }

        public (byte[] rxpacket, int result) RxPacket()
        {
            List<byte> rxpacket = new List<byte>();

            int result = Constans.COMM_TX_FAIL;
            int checksum = 0;
            int rx_length = 0;
            int wait_length = 6; // minimum length (HEADER0 HEADER1 ID LENGTH ERROR CHKSUM)

            while (true)
            {
                // Read the required number of bytes from the port
                byte[] readBytes = _controller.ReadPort(wait_length - rx_length);
                for (int i = 0; i < readBytes.Length; i++)
                    rxpacket.Add(readBytes[i]);
                rx_length = rxpacket.Count;

                if (rx_length >= wait_length)
                {
                    int idx = 0;
                    // find packet header
                    for (idx = 0; idx < rx_length - 1; idx++)
                    {
                        if (rxpacket[idx] == 0xFF && rxpacket[idx + 1] == 0xFF)
                            break;
                    }

                    if (idx == 0) // found at the beginning of the packet
                    {
                        if (rxpacket[PKT_ID] > 0xFD || rxpacket[PKT_LENGTH] > RXPACKET_MAX_LEN || rxpacket[PKT_ERROR] > 0x7F)
                        {
                            // unavailable ID or unavailable Length or unavailable Error
                            // remove the first byte in the packet
                            rxpacket.RemoveAt(0);
                            rx_length -= 1;
                            continue;
                        }

                        // re-calculate the exact length of the rx packet
                        if (wait_length != (rxpacket[PKT_LENGTH] + PKT_LENGTH + 1))
                        {
                            wait_length = rxpacket[PKT_LENGTH] + PKT_LENGTH + 1;
                            continue;
                        }

                        // calculate checksum
                        checksum = 0;
                        for (int i = 2; i < wait_length - 1; i++) // except header, checksum
                            checksum += rxpacket[i];
                        checksum = ~checksum & 0xFF;

                        // verify checksum
                        if (rxpacket[wait_length - 1] == checksum)
                            result = Constans.COMM_SUCCESS;
                        else
                            result = Constans.COMM_RX_CORRUPT;
                        break;
                    }
                    else
                    {
                        // remove unnecessary packets
                        rxpacket.RemoveRange(0, idx);
                        rx_length -= idx;
                    }
                }
                else
                {
                    throw new TimeoutException();
                }
            }
            
            return (rxpacket.ToArray(), result);
        }

        public (byte[]? rxpacket, int result, int error) TxRxPacket(byte[] txpacket)
        {
            byte[]? rxpacket = null;
            int error = 0;

            // tx packet
            int result = TxPacket(txpacket);
            if (result != Constans.COMM_SUCCESS)
                return (rxpacket, result, error);

            // (ID == Broadcast ID) == no need to wait for status packet or not available
            if (txpacket[PKT_ID] == Constans.BROADCAST_ID)
            {
                //portHandler.is_using = false;
                return (rxpacket, result, error);
            }

            // set packet timeout
            //if (txpacket[PKT_INSTRUCTION] == Constans.INST_READ)
            //    portHandler.SetPacketTimeout(txpacket[PKT_PARAMETER0 + 1] + 6);
            //else
            //    portHandler.SetPacketTimeout(6); // HEADER0 HEADER1 ID LENGTH ERROR CHECKSUM

            // rx packet
            while (true)
            {
                (rxpacket, result) = RxPacket();
                if (result != Constans.COMM_SUCCESS || txpacket[PKT_ID] == rxpacket[PKT_ID])
                    break;
            }

            if (result == Constans.COMM_SUCCESS && txpacket[PKT_ID] == rxpacket[PKT_ID])
                error = rxpacket[PKT_ERROR];

            return (rxpacket, result, error);
        }

        public (int modelNumber, int result, int error) Ping(byte stsId)
        {
            int modelNumber = 0;
            int error = 0;
            byte[] txpacket = new byte[6];

            if (stsId >= Constans.BROADCAST_ID)
                return (modelNumber, Constans.COMM_NOT_AVAILABLE, error);

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = 2;
            txpacket[PKT_INSTRUCTION] = Constans.INST_PING;

            (byte[]? rxpacket, int result, int rxError) = TxRxPacket(txpacket);

            if (result == Constans.COMM_SUCCESS)
            {
                (byte[] dataRead, int readResult, int readError) = ReadTxRx(stsId, 3, 2); // Address 3: Model Number
                if (readResult == Constans.COMM_SUCCESS)
                    modelNumber = StsMakeWord(dataRead[0], dataRead[1]);
                error = readError;
                result = readResult;
            }
            else
            {
                error = rxError;
            }

            return (modelNumber, result, error);
        }

        public (byte[] data, int result, int error) ReadTxRx(byte stsId, byte address, byte length)
        {
            byte[] txpacket = new byte[8];
            List<byte> data = new List<byte>();

            if (stsId >= Constans.BROADCAST_ID)
                return (data.ToArray(), Constans.COMM_NOT_AVAILABLE, 0);

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = 4;
            txpacket[PKT_INSTRUCTION] = Constans.INST_READ;
            txpacket[PKT_PARAMETER0 + 0] = address;
            txpacket[PKT_PARAMETER0 + 1] = length;

            (byte[]? rxpacket, int result, int error) = TxRxPacket(txpacket);

            rxpacket = rxpacket ?? throw new ArgumentNullException(nameof(rxpacket));

            if (result == Constans.COMM_SUCCESS)
            {
                error = rxpacket[PKT_ERROR];
                // Copy the requested data bytes from the response
                for (int i = 0; i < length; i++)
                    data.Add(rxpacket[PKT_PARAMETER0 + i]);
            }

            return (data.ToArray(), result, error);
        }

        public int Action(byte stsId)
        {
            byte[] txpacket = new byte[6];
            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = 2;
            txpacket[PKT_INSTRUCTION] = Constans.INST_ACTION;

            (_, int result, _) = TxRxPacket(txpacket);
            return result;
        }

        public int ReadTx(byte stsId, byte address, byte length)
        {
            byte[] txpacket = new byte[8];

            if (stsId >= Constans.BROADCAST_ID)
                return Constans.COMM_NOT_AVAILABLE;

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = 4;
            txpacket[PKT_INSTRUCTION] = Constans.INST_READ;
            txpacket[PKT_PARAMETER0 + 0] = address;
            txpacket[PKT_PARAMETER0 + 1] = length;

            int result = TxPacket(txpacket);

            return result;
        }

        public (byte[] data, int result, int error) ReadRx(byte stsId, byte length)
        {
            int result = Constans.COMM_TX_FAIL;
            int error = 0;
            byte[]? rxpacket = null;
            List<byte> data = new List<byte>();

            while (true)
            {
                (rxpacket, result) = RxPacket();
                if (result != Constans.COMM_SUCCESS || rxpacket[PKT_ID] == stsId)
                    break;
            }

            if (result == Constans.COMM_SUCCESS && rxpacket[PKT_ID] == stsId)
            {
                error = rxpacket[PKT_ERROR];
                for (int i = 0; i < length; i++)
                    data.Add(rxpacket[PKT_PARAMETER0 + i]);
            }
            
            return (data.ToArray(), result, error);
        }

        public (byte dataRead, int result, int error) Read1ByteRx(byte stsId)
        {
            (byte[] data, int result, int error) = ReadRx(stsId, 1);
            byte dataRead = (result == Constans.COMM_SUCCESS) ? data[0] : (byte)0;
            return (dataRead, result, error);
        }

        public (byte dataRead, int result, int error) Read1ByteTxRx(byte stsId, byte address)
        {
            (byte[] data, int result, int error) = ReadTxRx(stsId, address, 1);
            byte dataRead = (result == Constans.COMM_SUCCESS) ? data[0] : (byte)0;
            return (dataRead, result, error);
        }

        public (int dataRead, int result, int error) Read2ByteRx(byte stsId)
        {
            (byte[] data, int result, int error) = ReadRx(stsId, 2);
            int dataRead = (result == Constans.COMM_SUCCESS) ? StsMakeWord(data[0], data[1]) : 0;
            return (dataRead, result, error);
        }

        public (int dataRead, int result, int error) Read2ByteTxRx(byte stsId, byte address)
        {
            (byte[] data, int result, int error) = ReadTxRx(stsId, address, 2);
            int dataRead = (result == Constans.COMM_SUCCESS) ? StsMakeWord(data[0], data[1]) : 0;
            return (dataRead, result, error);
        }

        public (int dataRead, int result, int error) Read4ByteRx(byte stsId)
        {
            (byte[] data, int result, int error) = ReadRx(stsId, 4);
            int dataRead = (result == Constans.COMM_SUCCESS)
                ? StsMakeDWord(StsMakeWord(data[0], data[1]), StsMakeWord(data[2], data[3]))
                : 0;
            return (dataRead, result, error);
        }

        public (int dataRead, int result, int error) Read4ByteTxRx(byte stsId, byte address)
        {
            (byte[] data, int result, int error) = ReadTxRx(stsId, address, 4);
            int dataRead = (result == Constans.COMM_SUCCESS)
                ? StsMakeDWord(StsMakeWord(data[0], data[1]), StsMakeWord(data[2], data[3]))
                : 0;
            return (dataRead, result, error);
        }

        public int WriteTxOnly(byte stsId, byte address, byte length, byte[] data)
        {
            byte[] txpacket = new byte[length + 7];

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = (byte)(length + 3);
            txpacket[PKT_INSTRUCTION] = Constans.INST_WRITE;
            txpacket[PKT_PARAMETER0] = address;

            for (int i = 0; i < length; i++)
                txpacket[PKT_PARAMETER0 + 1 + i] = data[i];

            int result = TxPacket(txpacket);
            return result;
        }

        public (int result, int error) WriteTxRx(byte stsId, byte address, byte length, byte[] data)
        {
            byte[] txpacket = new byte[length + 7];

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = (byte)(length + 3);
            txpacket[PKT_INSTRUCTION] = Constans.INST_WRITE;
            txpacket[PKT_PARAMETER0] = address;

            for (int i = 0; i < length; i++)
                txpacket[PKT_PARAMETER0 + 1 + i] = data[i];

            (byte[]? rxpacket, int result, int error) = TxRxPacket(txpacket);
            return (result, error);
        }

        public int Write1ByteTxOnly(byte stsId, byte address, byte data)
        {
            return WriteTxOnly(stsId, address, 1, new byte[] { data });
        }

        public (int result, int error) Write1ByteTxRx(byte stsId, byte address, byte data)
        {
            return WriteTxRx(stsId, address, 1, new byte[] { data });
        }

        public int Write2ByteTxOnly(byte stsId, byte address, int data)
        {
            return WriteTxOnly(stsId, address, 2, new byte[] { (byte)StsLoByte(data), (byte)StsHiByte(data) });
        }

        public (int result, int error) Write2ByteTxRx(byte stsId, byte address, int data)
        {
            return WriteTxRx(stsId, address, 2, new byte[] { (byte)StsLoByte(data), (byte)StsHiByte(data) });
        }

        public int Write4ByteTxOnly(byte stsId, byte address, int data)
        {
            return WriteTxOnly(stsId, address, 4, new byte[]
            {
                (byte)StsLoByte(StsLoWord(data)),
                (byte)StsHiByte(StsLoWord(data)),
                (byte)StsLoByte(StsHiWord(data)),
                (byte)StsHiByte(StsHiWord(data))
            });
        }

        public (int result, int error) Write4ByteTxRx(byte stsId, byte address, int data)
        {
                return WriteTxRx(stsId, address, 4, new byte[]
            {
                (byte)StsLoByte(StsLoWord(data)),
                (byte)StsHiByte(StsLoWord(data)),
                (byte)StsLoByte(StsHiWord(data)),
                (byte)StsHiByte(StsHiWord(data))
            });
        }

        public int RegWriteTxOnly(byte stsId, byte address, byte length, byte[] data)
        {
            byte[] txpacket = new byte[length + 7];

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = (byte)(length + 3);
            txpacket[PKT_INSTRUCTION] = Constans.INST_REG_WRITE;
            txpacket[PKT_PARAMETER0] = address;

            for (int i = 0; i < length; i++)
                txpacket[PKT_PARAMETER0 + 1 + i] = data[i];

            int result = TxPacket(txpacket);
            return result;
        }

        public (int result, int error) RegWriteTxRx(byte stsId, byte address, byte length, byte[] data)
        {
            byte[] txpacket = new byte[length + 7];

            txpacket[PKT_ID] = stsId;
            txpacket[PKT_LENGTH] = (byte)(length + 3);
            txpacket[PKT_INSTRUCTION] = Constans.INST_REG_WRITE;
            txpacket[PKT_PARAMETER0] = address;

            for (int i = 0; i < length; i++)
                txpacket[PKT_PARAMETER0 + 1 + i] = data[i];

            (_, int result, int error) = TxRxPacket(txpacket);
            return (result, error);
        }

        public int SyncReadTx(byte startAddress, byte dataLength, List<byte> param, int paramLength)
        {
            byte[] txpacket = new byte[paramLength + 8];

            txpacket[PKT_ID] = Constans.BROADCAST_ID;
            txpacket[PKT_LENGTH] = (byte)(paramLength + 4);
            txpacket[PKT_INSTRUCTION] = Constans.INST_SYNC_READ;
            txpacket[PKT_PARAMETER0 + 0] = startAddress;
            txpacket[PKT_PARAMETER0 + 1] = dataLength;

            for (int i = 0; i < paramLength; i++)
                txpacket[PKT_PARAMETER0 + 2 + i] = param[i];

            int result = TxPacket(txpacket);
            return result;
        }

        public (int result, byte[] rxpacket) SyncReadRx(int dataLength, int paramLength)
        {
            int waitLength = (6 + dataLength) * paramLength;
            List<byte> rxpacket = new List<byte>();
            int rx_length = 0;
            int result = Constans.COMM_TX_FAIL;

            while (true)
            {
                byte[] readBytes = _controller.ReadPort(waitLength - rx_length);
                rxpacket.AddRange(readBytes);
                rx_length = rxpacket.Count;
                if (rx_length >= waitLength)
                {
                    result = Constans.COMM_SUCCESS;
                    break;
                }
            }
            return (result, rxpacket.ToArray());
        }

        public int SyncWriteTxOnly(byte startAddress, byte dataLength, byte[] param, int paramLength)
        {
            byte[] txpacket = new byte[paramLength + 8];

            txpacket[PKT_ID] = Constans.BROADCAST_ID;
            txpacket[PKT_LENGTH] = (byte)(paramLength + 4);
            txpacket[PKT_INSTRUCTION] = Constans.INST_SYNC_WRITE;
            txpacket[PKT_PARAMETER0 + 0] = startAddress;
            txpacket[PKT_PARAMETER0 + 1] = dataLength;

            for (int i = 0; i < paramLength; i++)
                txpacket[PKT_PARAMETER0 + 2 + i] = param[i];

            (_, int result, _) = TxRxPacket(txpacket);
            return result;
        }

        public (int result, int error) WritePosEx(byte stsId, int position, int speed, byte acc)
        {
            var txpacket = new List<byte>
            {
                acc,
                (byte)this.StsLoByte(position),
                (byte)this.StsHiByte(position),
                0,
                0,
                (byte)this.StsLoByte(speed),
                (byte)this.StsHiByte(speed)
            };
            return this.WriteTxRx(stsId, Constans.STS_ACC, (byte)txpacket.Count, txpacket.ToArray());
        }

        public (int position, int result, int error) ReadPos(byte stsId)
        {
            var (stsPresentPosition, stsCommResult, stsError) = this.Read2ByteTxRx(stsId, Constans.STS_PRESENT_POSITION_L);
            return (this.StsToHost(stsPresentPosition, 15), stsCommResult, stsError);
        }

        public (int speed, int result, int error) ReadSpeed(byte stsId)
        {
            var (stsPresentSpeed, stsCommResult, stsError) = this.Read2ByteTxRx(stsId, Constans.STS_PRESENT_SPEED_L);
            return (this.StsToHost(stsPresentSpeed, 15), stsCommResult, stsError);
        }

        public (int position, int speed, int result, int error) ReadPosSpeed(byte stsId)
        {
            var (posSpeed, stsCommResult, stsError) = this.Read4ByteTxRx(stsId, Constans.STS_PRESENT_POSITION_L);
            int position = this.StsLoWord(posSpeed);
            int speed = this.StsHiWord(posSpeed);
            return (this.StsToHost(position, 15), this.StsToHost(speed, 15), stsCommResult, stsError);
        }

        public (byte moving, int result, int error) ReadMoving(byte stsId)
        {
            var (moving, stsCommResult, stsError) = this.Read1ByteTxRx(stsId, Constans.STS_MOVING);
            return (moving, stsCommResult, stsError);
        }

        //public bool SyncWritePosEx(byte stsId, int position, int speed, byte acc)
        //{
        //    var txpacket = new List<byte>
        //{
        //    acc,
        //    (byte)this.StsLoByte(position),
        //    (byte)this.StsHiByte(position),
        //    0, 0,
        //    (byte)this.StsLoByte(speed),
        //    (byte)this.StsHiByte(speed)
        //};
        //    return this.groupSyncWrite.AddParam(stsId, txpacket);
        //}

        public (int result, int error) RegWritePosEx(byte stsId, int position, int speed, byte acc)
        {
            var txpacket = new List<byte>
            {
                acc,
                (byte)this.StsLoByte(position),
                (byte)this.StsHiByte(position),
                0, 
                0,
                (byte)this.StsLoByte(speed),
                (byte)this.StsHiByte(speed)
            };
            return this.RegWriteTxRx(stsId, Constans.STS_ACC, (byte)txpacket.Count, txpacket.ToArray());
        }

        public int RegAction()
        {
            return this.Action(Constans.BROADCAST_ID);
        }

        public (int result, int error) WheelMode(byte stsId)
        {
            return this.Write1ByteTxRx(stsId, Constans.STS_MODE, 1);
        }

        public (int result, int error) WriteSpec(byte stsId, int speed, byte acc)
        {
            speed = this.StsToScs(speed, 15);
            var txpacket = new List<byte>
            {
                acc, 
                0, 
                0, 
                0, 
                0,
                (byte)this.StsLoByte(speed),
                (byte)this.StsHiByte(speed)
            };
            return this.WriteTxRx(stsId, Constans.STS_ACC, (byte)txpacket.Count, txpacket.ToArray());
        }

        public (int result, int error) LockEprom(byte stsId)
        {
            return this.Write1ByteTxRx(stsId, Constans.STS_LOCK, 1);
        }

        public (int result, int error) UnLockEprom(byte stsId)
        {
            return this.Write1ByteTxRx(stsId, Constans.STS_LOCK, 0);
        }
    }
}
