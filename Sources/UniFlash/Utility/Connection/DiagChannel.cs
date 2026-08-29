using iReverse_Unisoc_Ultimate.Utility.Connection.API;
using System;

namespace iReverse_Unisoc_Ultimate
{
    namespace Utility.Connection
    {
        internal static class DiagChannel
        {
            private static readonly object SyncRoot = new object();

            public static PhoneCommandAPI.SP_HANDLE hDiagPhone;
            public static byte[] ChannelBuffer = new byte[1024];

            public static bool IsConnected
            {
                get
                {
                    lock (SyncRoot)
                    {
                        return hDiagPhone.Value != IntPtr.Zero;
                    }
                }
            }

            public static bool DiagConnect(string portCom, out int nativeResult)
            {
                nativeResult = -1;

                int port;
                if (!int.TryParse(portCom, out port) || port <= 0)
                    return false;

                lock (SyncRoot)
                {
                    if (hDiagPhone.Value != IntPtr.Zero)
                        return true;

                    hDiagPhone = PhoneCommandAPI.SP_CreatePhone(IntPtr.Zero);
                    if (hDiagPhone.Value == IntPtr.Zero)
                        return false;

                    PhoneCommandAPI.CHANNEL_ATTRIBUTE openArgument = new PhoneCommandAPI.CHANNEL_ATTRIBUTE();
                    openArgument.ChannelType = PhoneCommandAPI.CHANNEL_TYPE.CHANNEL_TYPE_COM;
                    openArgument.Com.dwPortNum = (uint)port;
                    openArgument.Com.dwBaudRate = 115200;

                    nativeResult = PhoneCommandAPI.SP_BeginPhoneTest(
                        hDiagPhone,
                        ref openArgument
                    );

                    Console.WriteLine(
                        "Begin Diag Channel: " + nativeResult + " USB Port COM" + port
                    );

                    if (nativeResult != 0)
                    {
                        ReleaseHandleNoThrow();
                        return false;
                    }

                    return true;
                }
            }

            public static void DiagClose()
            {
                lock (SyncRoot)
                {
                    ReleaseHandleNoThrow();
                }
            }

            private static void ReleaseHandleNoThrow()
            {
                if (hDiagPhone.Value == IntPtr.Zero)
                    return;

                try
                {
                    PhoneCommandAPI.SP_EndPhoneTest(hDiagPhone);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SP_EndPhoneTest: " + ex.Message);
                }

                try
                {
                    hDiagPhone = PhoneCommandAPI.SP_ReleasePhone(hDiagPhone);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SP_ReleasePhone: " + ex.Message);
                    hDiagPhone = default(PhoneCommandAPI.SP_HANDLE);
                }

                hDiagPhone = default(PhoneCommandAPI.SP_HANDLE);
            }

            public static int WriteDiag(byte[] lpvalue)
            {
                if (!IsConnected || lpvalue == null || lpvalue.Length == 0)
                    return -1;

                return PhoneCommandAPI.SP_Write(
                    hDiagPhone,
                    lpvalue,
                    (ulong)lpvalue.Length
                );
            }
        }
    }
}
