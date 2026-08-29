using System;
using System.Runtime.InteropServices;

namespace iReverse_Unisoc_Ultimate
{
    namespace Utility.Connection.API
    {
        internal static class PhoneCommandAPI
        {
            #region PhoneCommand

            private const string LibPhoneCommand = @"Data\DeviceApi\PhoneCommand.dll";

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern SP_HANDLE SP_CreatePhone(IntPtr pLogUtil);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_BeginPhoneTest(
                SP_HANDLE hDiagPhone,
                ref CHANNEL_ATTRIBUTE pOpenArgument
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern void SP_EndPhoneTest(SP_HANDLE hDiagPhone);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern SP_HANDLE SP_ReleasePhone(SP_HANDLE hDiagPhone);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_RestartPhone(SP_HANDLE hDiagPhone, RM_MODE_ENUM ePhoneMode);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_ReadSN(
                SP_HANDLE hDiagPhone,
                bool sn1,
                IntPtr pSN,
                uint uLen
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_SendATCommand(
                SP_HANDLE hDiagPhone,
                byte[] lpATCommand,
                bool bWantReply,
                byte[] lpReplyString,
                uint ulReplyStringLen,
                ref uint pulResponseLen,
                uint ulTimeOut
            );

            // Deklarasi fungsi SP_ReadNV
            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_GetAPVersion(
                SP_HANDLE hDiagPhone,
                IntPtr lpBuff,
                uint ulBuffLen
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_LoadProductData(
                SP_HANDLE hDiagPhone,
                byte[] ImeiNvID,
                byte[] Output
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_btLoadAddr(SP_HANDLE hDiagPhone, byte[] BTAddr);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_wifiLoadAddr(SP_HANDLE hDiagPhone, byte[] WIFIAddr);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_SendAndRecvDiagPackage(
                SP_HANDLE hDiagPhone,
                byte[] lpValue,
                ulong nbrOfBytesToWrite
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_Write(
                SP_HANDLE hDiagPhone,
                byte[] lpValue,
                ulong nbrOfBytesToWrite
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_Read(
                SP_HANDLE hDiagPhone,
                byte[] lpValue,
                ulong nbrOfBytesToRead,
                ulong ulTimeOut = 1000
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_ReadNV(
                SP_HANDLE hDiagPhone,
                ushort uNvID,
                IntPtr lpData,
                uint ulDataLen,
                ref uint pulDataLen
            );

            public struct SP_HANDLE
            {
                public IntPtr Value;

                public void CleanUp()
                {
                    if (Value != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(Value);
                    }
                }
            }

            public enum RM_MODE_ENUM
            {
                RM_NORMAL_MODE = 0x0
            }

            public enum CHANNEL_TYPE
            {
                CHANNEL_TYPE_COM = 0,
                CHANNEL_TYPE_SOCKET = 1,
                CHANNEL_TYPE_FILE = 2,
                CHANNEL_TYPE_USBMON = 3
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct CHANNEL_ATTRIBUTE
            {
                public CHANNEL_TYPE ChannelType;
                public COM_CHANNEL_ATTRIBUTE Com;
                public SOCKET_CHANNEL_ATTRIBUTE Socket;
                public FILE_CHANNEL_ATTRIBUTE File;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct COM_CHANNEL_ATTRIBUTE
            {
                public uint dwPortNum;
                public uint dwBaudRate;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SOCKET_CHANNEL_ATTRIBUTE
            {
                public uint dwPort;
                public uint dwIP;
                public uint dwFlag;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct FILE_CHANNEL_ATTRIBUTE
            {
                public uint dwPackSize;
                public uint dwPackFreq;
                public string pFilePath;
            }

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_SaveProductData(
                SP_HANDLE hDiagPhone,
                byte[] param_1,
                int[] param_2
            );

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_PowerOff(SP_HANDLE hDiagPhone);

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_CustomerPhoneOp(
                SP_HANDLE hDiagPhone,
                CUSTOMER_PHONE_STATE_OPER eOper
            );

            public enum CUSTOMER_PHONE_STATE_OPER
            {
                CUSTOMER_DISK_FORMAT = 0,
                CUSTOMER_FACTORY_RESET,
                CUSTOMER_TCARD_CLEAR
            }

            public const int MAX_IMEI_STR_LENGTH = 16;
            public const int MAX_IMEI_NV_LENGTH = 8;
            public const int MAX_BT_ADDR_STR_LENGTH = 13;
            public const int MAX_BT_ADDR_NV_LENGTH = 6;
            public const int MAX_WIFI_ADDR_STR_LENGTH = 13;
            public const int MAX_WIFI_ADDR_NV_LENGTH = 6;
            public const ushort NVID_IMEI1 = 0x5;
            public const ushort NVID_IMEI2 = 0x179;
            public const ushort NVID_SIMLOCK_SIGN = 0x1A3;
            public const ushort NVID_SIMLOCK_DATA = 0x1A4;
            public const ushort NVID_SIM_LOCK_CUSTOMIZE_DATA = 0x1F0;
            public const ushort NVID_SIM_LOCK_USER_DATA = 0x1F1;
            public const ushort NVID_SIM_LOCK_CONTROL_KEY = 0x1F2;
            public const ushort NVID_SIM_LOCK_STORAGE_KEY = 0x1F4;
            public const ushort NVID_NV_PARAM_TYPE_SIM_CFG1 = 0x7E4;
            // Anti-Crack / ATS specific NV IDs (Transsion / Unisoc)
            public const ushort NVID_ANTI_CRACK_FLAG      = 0x22F;  // 559  - ATS lock flag
            public const ushort NVID_SECURITY_STATE       = 0x230;  // 560  - Security state word
            public const ushort NVID_SIM_LOCK_EX_DATA     = 0x1F3;  // 499  - Extended SimLock data
            public const ushort NVID_SIM_CFG2             = 0x7E5;  // 2021 - SIM config block 2
            public const ushort NVID_ANTI_CRACK_EXT       = 0x4D2;  // 1234 - Anti-Crack extension (Transsion)

            [DllImport(
                LibPhoneCommand,
                CharSet = CharSet.Auto,
                CallingConvention = CallingConvention.StdCall
            )]
            public static extern int SP_WriteNV(
                SP_HANDLE hDiagPhone,
                ushort uNvID,
                IntPtr lpData,
                uint ulDataLen
            );

            [DllImport(LibPhoneCommand, CallingConvention = CallingConvention.StdCall)]
            public static extern int SP_ReadImei(
                SP_HANDLE hDiagPhone,
                ushort ImeiNvID,
                byte[] IMEI
            );

            [DllImport(LibPhoneCommand, CallingConvention = CallingConvention.StdCall)]
            public static extern int SP_WriteImei(
                SP_HANDLE hDiagPhone,
                ushort ImeiNvID,
                string imei
            );

            #endregion
        }
    }
}
