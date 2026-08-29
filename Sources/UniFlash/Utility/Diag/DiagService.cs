using iReverse_Unisoc_Ultimate.Utility.Connection;
using iReverse_Unisoc_Ultimate.Utility.Connection.API;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace iReverse_Unisoc_Ultimate.UniFlash.Utility.Diag
{
    internal sealed class DiagService
    {
        private const uint DefaultTimeout = 5000;

        public bool IsConnected { get { return DiagChannel.IsConnected; } }

        public DiagResult Connect(string port)
        {
            int nativeResult;
            if (DiagChannel.DiagConnect(port, out nativeResult))
                return DiagResult.Ok("Connect", "Canal Diag conectado.");

            return DiagResult.Fail("Connect", nativeResult, "Falha ao abrir o canal Diag.");
        }

        public void Disconnect()
        {
            DiagChannel.DiagClose();
        }

        public DiagResult ReadApVersion(out string version)
        {
            version = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("ReadApVersion", -1, "Canal Diag não conectado.");

            IntPtr buffer = IntPtr.Zero;
            try
            {
                const int capacity = 1024;
                buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(capacity);
                int result = PhoneCommandAPI.SP_GetAPVersion(
                    DiagChannel.hDiagPhone, buffer, capacity);

                if (result != 0)
                    return DiagResult.Fail("ReadApVersion", result, "Falha ao obter a versão do AP.");

                version = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
                return DiagResult.Ok("ReadApVersion", version);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }

        public DiagResult ReadImei(ushort nvId, out string imei)
        {
            imei = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("ReadImei", -1, "Canal Diag não conectado.");

            byte[] data = new byte[PhoneCommandAPI.MAX_IMEI_STR_LENGTH];
            int result = PhoneCommandAPI.SP_ReadImei(DiagChannel.hDiagPhone, nvId, data);
            if (result != 0)
                return DiagResult.Fail("ReadImei", result, "Falha ao ler IMEI.");

            imei = Encoding.ASCII.GetString(data).Trim('\0', ' ', '\r', '\n');
            return DiagResult.Ok("ReadImei", imei);
        }

        public DiagResult SendAt(string command, out string response)
        {
            response = string.Empty;
            if (!IsConnected)
                return DiagResult.Fail("SendAt", -1, "Canal Diag não conectado.");
            if (string.IsNullOrWhiteSpace(command))
                return DiagResult.Fail("SendAt", -2, "Comando AT vazio.");

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);
            byte[] reply = new byte[1024];
            uint replyLength = 0;

            int result = PhoneCommandAPI.SP_SendATCommand(
                DiagChannel.hDiagPhone,
                commandBytes,
                true,
                reply,
                (uint)reply.Length,
                ref replyLength,
                DefaultTimeout);

            if (replyLength > reply.Length)
                replyLength = (uint)reply.Length;

            response = Encoding.ASCII.GetString(reply, 0, (int)replyLength);
            response = Regex.Replace(response, "[\\r\\n]", string.Empty);

            if (result != 0)
                return DiagResult.Fail("SendAt", result, response);

            return DiagResult.Ok("SendAt", response);
        }
    }
}
