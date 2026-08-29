using iReverse_Unisoc_Ultimate.Utility.Connection.API;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace iReverse_Unisoc_Ultimate.Utility.Connection
{
    internal static class DiagService
    {
        public static bool IsConnected
        {
            get { return DiagChannel.IsConnected; }
        }

        public static DiagResult Connect(string portCom)
        {
            if (string.IsNullOrWhiteSpace(portCom))
                return DiagResult.Fail("Connect", -1, "COM port is empty.");

            int port;
            if (!int.TryParse(portCom, out port) || port <= 0)
                return DiagResult.Fail("Connect", -1, "Invalid COM port: " + portCom);

            int nativeResult;
            bool connected = DiagChannel.DiagConnect(portCom, out nativeResult);
            if (!connected)
                return DiagResult.Fail("Connect", nativeResult, "Failed to open Diag channel on COM" + portCom + ".");

            return DiagResult.Ok("Connect", "Diag channel connected on COM" + portCom + ".");
        }

        public static DiagResult Disconnect()
        {
            try
            {
                DiagChannel.DiagClose();
                return DiagResult.Ok("Disconnect", "Diag channel closed.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Disconnect", -1, ex.Message);
            }
        }

        public static DiagResult ReadApVersion(out string version)
        {
            version = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("Read AP Version", -1, "Diag channel is not connected.");

            IntPtr buffer = IntPtr.Zero;
            try
            {
                const int bufferLength = 1024;
                buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(bufferLength);
                int result = PhoneCommandAPI.SP_GetAPVersion(
                    DiagChannel.hDiagPhone,
                    buffer,
                    bufferLength
                );

                if (result != 0)
                    return DiagResult.Fail("Read AP Version", result, "SP_GetAPVersion failed.");

                version = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
                return DiagResult.Ok("Read AP Version", version);
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Read AP Version", -1, ex.Message);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }

        public static DiagResult ReadImei(ushort nvId, out string imei)
        {
            imei = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("Read IMEI", -1, "Diag channel is not connected.");

            byte[] buffer = new byte[PhoneCommandAPI.MAX_IMEI_STR_LENGTH];
            try
            {
                int result = PhoneCommandAPI.SP_ReadImei(DiagChannel.hDiagPhone, nvId, buffer);
                if (result != 0)
                    return DiagResult.Fail("Read IMEI", result, "SP_ReadImei failed.");

                int length = Array.IndexOf<byte>(buffer, 0);
                if (length < 0) length = buffer.Length;
                imei = Encoding.ASCII.GetString(buffer, 0, length).Trim();
                return DiagResult.Ok("Read IMEI", imei);
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Read IMEI", -1, ex.Message);
            }
        }

        public static DiagResult SendAt(string command, out string response)
        {
            response = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("Send AT", -1, "Diag channel is not connected.");
            if (string.IsNullOrWhiteSpace(command))
                return DiagResult.Fail("Send AT", -1, "AT command is empty.");

            try
            {
                byte[] commandBytes = Encoding.ASCII.GetBytes(command.Trim());
                byte[] reply = new byte[1024];
                uint replyLength = 0;
                int result = PhoneCommandAPI.SP_SendATCommand(
                    DiagChannel.hDiagPhone,
                    commandBytes,
                    true,
                    reply,
                    (uint)reply.Length,
                    ref replyLength,
                    5000
                );

                if (replyLength > reply.Length)
                    replyLength = (uint)reply.Length;

                response = Encoding.ASCII.GetString(reply, 0, (int)replyLength);
                response = Regex.Replace(response, "[\\r\\n]|OK", string.Empty).Trim();

                if (result != 0)
                    return DiagResult.Fail("Send AT", result, response);

                return DiagResult.Ok("Send AT", response);
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Send AT", -1, ex.Message);
            }
        }

        public static DiagResult Write(byte[] data)
        {
            if (!IsConnected)
                return DiagResult.Fail("Diag Write", -1, "Diag channel is not connected.");
            if (data == null || data.Length == 0)
                return DiagResult.Fail("Diag Write", -1, "Payload is empty.");

            try
            {
                int result = PhoneCommandAPI.SP_Write(
                    DiagChannel.hDiagPhone,
                    data,
                    (ulong)data.Length
                );
                if (result != 0)
                    return DiagResult.Fail("Diag Write", result, "SP_Write failed.");

                return DiagResult.Ok("Diag Write", "Data written to Diag channel.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Diag Write", -1, ex.Message);
            }
        }
    }
}
