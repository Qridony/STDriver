using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STDriver
{
    public class Constans
    {
        // Baud rate definitions
        public const int STS_1M = 0;
        public const int STS_0_5M = 1;
        public const int STS_250K = 2;
        public const int STS_128K = 3;
        public const int STS_115200 = 4;
        public const int STS_76800 = 5;
        public const int STS_57600 = 6;
        public const int STS_38400 = 7;

        // Memory table definitions
        // EPROM (Read Only)
        public const int STS_MODEL_L = 3;
        public const int STS_MODEL_H = 4;

        // EPROM (Read and Write)
        public const int STS_ID = 5;
        public const int STS_BAUD_RATE = 6;
        public const int STS_MIN_ANGLE_LIMIT_L = 9;
        public const int STS_MIN_ANGLE_LIMIT_H = 10;
        public const int STS_MAX_ANGLE_LIMIT_L = 11;
        public const int STS_MAX_ANGLE_LIMIT_H = 12;
        public const int STS_CW_DEAD = 26;
        public const int STS_CCW_DEAD = 27;
        public const int STS_OFS_L = 31;
        public const int STS_OFS_H = 32;
        public const int STS_MODE = 33;

        // SRAM (Read and Write)
        public const int STS_TORQUE_ENABLE = 40;
        public const int STS_ACC = 41;
        public const int STS_GOAL_POSITION_L = 42;
        public const int STS_GOAL_POSITION_H = 43;
        public const int STS_GOAL_TIME_L = 44;
        public const int STS_GOAL_TIME_H = 45;
        public const int STS_GOAL_SPEED_L = 46;
        public const int STS_GOAL_SPEED_H = 47;
        public const int STS_LOCK = 55;

        // SRAM (Read Only)
        public const int STS_PRESENT_POSITION_L = 56;
        public const int STS_PRESENT_POSITION_H = 57;
        public const int STS_PRESENT_SPEED_L = 58;
        public const int STS_PRESENT_SPEED_H = 59;
        public const int STS_PRESENT_LOAD_L = 60;
        public const int STS_PRESENT_LOAD_H = 61;
        public const int STS_PRESENT_VOLTAGE = 62;
        public const int STS_PRESENT_TEMPERATURE = 63;
        public const int STS_MOVING = 66;
        public const int STS_PRESENT_CURRENT_L = 69;
        public const int STS_PRESENT_CURRENT_H = 70;


        // Servo Defs
        public const byte BROADCAST_ID = 0xFE;  // 254
        public const byte MAX_ID = 0xFC;  // 252
        public const int STS_END = 0;

        // Instruction for STS Protocol
        public const int INST_PING = 1;
        public const int INST_READ = 2;
        public const int INST_WRITE = 3;
        public const int INST_REG_WRITE = 4;
        public const int INST_ACTION = 5;
        public const int INST_SYNC_WRITE = 131; // 0x83
        public const int INST_SYNC_READ = 130;  // 0x82

        // Communication Result
        public const int COMM_SUCCESS = 0;      // tx or rx packet communication success
        public const int COMM_PORT_BUSY = -1;   // Port is busy (in use)
        public const int COMM_TX_FAIL = -2;     // Failed transmit instruction packet
        public const int COMM_RX_FAIL = -3;     // Failed get status packet
        public const int COMM_TX_ERROR = -4;    // Incorrect instruction packet
        public const int COMM_RX_WAITING = -5;  // Now recieving status packet
        public const int COMM_RX_TIMEOUT = -6;  // There is no status packet
        public const int COMM_RX_CORRUPT = -7;  // Incorrect status packet
        public const int COMM_NOT_AVAILABLE = -9;
    }
}
