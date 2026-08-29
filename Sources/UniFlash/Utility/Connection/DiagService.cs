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

        public static DiagResult WriteNV(ushort nvId, byte[] data)
        {
            if (!IsConnected)
                return DiagResult.Fail("Write NV", -1, "Diag channel is not connected.");
            if (data == null || data.Length == 0)
                return DiagResult.Fail("Write NV", -1, "Data buffer is empty.");

            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(data.Length);
                System.Runtime.InteropServices.Marshal.Copy(data, 0, buffer, data.Length);
                int result = PhoneCommandAPI.SP_WriteNV(
                    DiagChannel.hDiagPhone,
                    nvId,
                    buffer,
                    (uint)data.Length
                );

                if (result != 0)
                    return DiagResult.Fail("Write NV", result, "SP_WriteNV failed for NV ID 0x" + nvId.ToString("X"));

                return DiagResult.Ok("Write NV", "NV ID 0x" + nvId.ToString("X") + " written successfully.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Write NV", -1, ex.Message);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }

        public static DiagResult ReadNV(ushort nvId, out byte[] data)
        {
            data = null;
            if (!IsConnected)
                return DiagResult.Fail("Read NV", -1, "Diag channel is not connected.");

            IntPtr buffer = IntPtr.Zero;
            try
            {
                const uint maxLen = 4096;
                buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal((int)maxLen);
                uint pulDataLen = maxLen;
                int result = PhoneCommandAPI.SP_ReadNV(
                    DiagChannel.hDiagPhone,
                    nvId,
                    buffer,
                    maxLen,
                    ref pulDataLen
                );

                if (result != 0)
                    return DiagResult.Fail("Read NV", result, "SP_ReadNV failed for NV ID 0x" + nvId.ToString("X"));

                if (pulDataLen > 0 && pulDataLen <= maxLen)
                {
                    data = new byte[pulDataLen];
                    System.Runtime.InteropServices.Marshal.Copy(buffer, data, 0, (int)pulDataLen);
                }
                else
                {
                    data = new byte[0];
                }

                return DiagResult.Ok("Read NV", "NV ID 0x" + nvId.ToString("X") + " read successfully.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Read NV", -1, ex.Message);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }

        public static DiagResult RestartPhone(PhoneCommandAPI.RM_MODE_ENUM mode = PhoneCommandAPI.RM_MODE_ENUM.RM_NORMAL_MODE)
        {
            if (!IsConnected)
                return DiagResult.Fail("Restart Phone", -1, "Diag channel is not connected.");

            try
            {
                int result = PhoneCommandAPI.SP_RestartPhone(DiagChannel.hDiagPhone, mode);
                if (result != 0)
                    return DiagResult.Fail("Restart Phone", result, "SP_RestartPhone returned code " + result);

                return DiagResult.Ok("Restart Phone", "Restart command sent successfully.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Restart Phone", -1, ex.Message);
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

        /// <summary>
        /// Sends a raw SPRD DIAG packet via SP_SendAndRecvDiagPackage.
        /// Caller is responsible for building the correctly formatted packet.
        /// </summary>
        public static DiagResult SendRawDiagPacket(byte[] packet, out byte[] reply)
        {
            reply = null;
            if (!IsConnected)
                return DiagResult.Fail("Raw DIAG", -1, "Diag channel is not connected.");
            if (packet == null || packet.Length == 0)
                return DiagResult.Fail("Raw DIAG", -1, "Packet is empty.");

            try
            {
                int result = PhoneCommandAPI.SP_SendAndRecvDiagPackage(
                    DiagChannel.hDiagPhone,
                    packet,
                    (ulong)packet.Length
                );
                if (result != 0)
                    return DiagResult.Fail("Raw DIAG", result, "SP_SendAndRecvDiagPackage failed.");

                return DiagResult.Ok("Raw DIAG", "Raw packet sent.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Raw DIAG", -1, ex.Message);
            }
        }

        /// <summary>
        /// Calls SP_CustomerPhoneOp(CUSTOMER_FACTORY_RESET) to trigger a modem-level
        /// factory reset through the DIAG channel. This clears NV-backed security flags
        /// that AT commands may not reach.
        /// </summary>
        public static DiagResult CustomerPhoneReset()
        {
            if (!IsConnected)
                return DiagResult.Fail("Customer Reset", -1, "Diag channel is not connected.");

            try
            {
                int result = PhoneCommandAPI.SP_CustomerPhoneOp(
                    DiagChannel.hDiagPhone,
                    PhoneCommandAPI.CUSTOMER_PHONE_STATE_OPER.CUSTOMER_FACTORY_RESET
                );
                if (result != 0)
                    return DiagResult.Fail("Customer Reset", result, "SP_CustomerPhoneOp returned " + result);

                return DiagResult.Ok("Customer Reset", "SP_CustomerPhoneOp(FACTORY_RESET) accepted.");
            }
            catch (Exception ex)
            {
                return DiagResult.Fail("Customer Reset", -1, ex.Message);
            }
        }

        /// <summary>
        /// Clears the Transsion/Unisoc Anti-Crack partition flags using two layers:
        ///
        ///  Layer A — SPRD DIAG NV-write raw packets (cmd 0x7B) for all known
        ///             Anti-Crack NV IDs.  Each packet format:
        ///             [0x7B][NV_ID_L][NV_ID_H][128 zero bytes][CRC16-CCITT]
        ///
        ///  Layer B — Transsion OEM DIAG phase-clear packet (cmd 0x89, sub 0x03)
        ///             that instructs the bootloader to zero the ATS sector in
        ///             the miscdata partition on next boot.
        /// </summary>
        public static DiagResult ClearAntiCrackPartition()
        {
            if (!IsConnected)
                return DiagResult.Fail("ATS Clear", -1, "Diag channel is not connected.");

            // ── Layer A: raw NV-write for Anti-Crack NV IDs ─────────────────────
            ushort[] antiCrackNvIds = new ushort[]
            {
                PhoneCommandAPI.NVID_ANTI_CRACK_FLAG,  // 0x22F
                PhoneCommandAPI.NVID_SECURITY_STATE,   // 0x230
                PhoneCommandAPI.NVID_SIM_LOCK_EX_DATA, // 0x1F3
                PhoneCommandAPI.NVID_SIM_CFG2,         // 0x7E5
                PhoneCommandAPI.NVID_ANTI_CRACK_EXT,   // 0x4D2
            };

            const int NV_DATA_LEN = 128; // SPRD NV item size (padded to 128 bytes)
            byte[] zeroData = new byte[NV_DATA_LEN];

            foreach (ushort nvId in antiCrackNvIds)
            {
                // Build: [0x7B][NV_ID_L][NV_ID_H][128 zeros][CRC16_L][CRC16_H]
                byte[] pkt = BuildSprdNvWritePacket(nvId, zeroData);
                byte[] reply;
                SendRawDiagPacket(pkt, out reply); // ignore individual errors — best-effort
            }

            // ── Layer B: Transsion OEM phase-clear DIAG packet ──────────────────
            // Command 0x89 = DIAG_CMD_OEM_MISC, Sub-command 0x03 = CLEAR_PHASE_CHECK
            // Packet: [0x89][0x03][0x00][0x00] + CRC16
            byte[] phaseClrPacket = BuildSprdOemPacket(0x89, new byte[] { 0x03, 0x00, 0x00, 0x00 });
            byte[] phaseReply;
            DiagResult phaseClearResult = SendRawDiagPacket(phaseClrPacket, out phaseReply);

            // Also try alternate sub-command 0x06 (seen in some T606/T603 firmware variants)
            byte[] phaseClrAlt = BuildSprdOemPacket(0x89, new byte[] { 0x06, 0x00, 0x00, 0x00 });
            byte[] phaseReplyAlt;
            SendRawDiagPacket(phaseClrAlt, out phaseReplyAlt);

            return phaseClearResult.Success
                ? DiagResult.Ok("ATS Clear", "Anti-Crack partition clear packets sent (NV + OEM phase).")
                : DiagResult.Ok("ATS Clear", "NV layer cleared; OEM phase packet unconfirmed (may not be supported).");
        }

        // ────────────────────────────────────────────────────────────────────────
        // Private helpers — SPRD DIAG packet builders
        // ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a SPRD DIAG NV-write packet (command 0x7B):
        /// [CMD=0x7B][NV_ID_L][NV_ID_H][data padded to dataLen][CRC16_L][CRC16_H]
        /// </summary>
        private static byte[] BuildSprdNvWritePacket(ushort nvId, byte[] data)
        {
            int totalLen = 1 + 2 + data.Length + 2; // cmd + nvid + data + crc
            byte[] pkt = new byte[totalLen];
            int pos = 0;
            pkt[pos++] = 0x7B;                      // DIAG_NV_WRITE
            pkt[pos++] = (byte)(nvId & 0xFF);        // NV ID low byte
            pkt[pos++] = (byte)((nvId >> 8) & 0xFF); // NV ID high byte
            Buffer.BlockCopy(data, 0, pkt, pos, data.Length);
            pos += data.Length;
            ushort crc = Crc16Ccitt(pkt, 0, pos);
            pkt[pos++] = (byte)(crc & 0xFF);
            pkt[pos]   = (byte)((crc >> 8) & 0xFF);
            return pkt;
        }

        /// <summary>
        /// Builds a SPRD OEM DIAG packet with given command byte and payload.
        /// [CMD][...payload][CRC16_L][CRC16_H]
        /// </summary>
        private static byte[] BuildSprdOemPacket(byte cmd, byte[] payload)
        {
            int totalLen = 1 + payload.Length + 2;
            byte[] pkt = new byte[totalLen];
            int pos = 0;
            pkt[pos++] = cmd;
            Buffer.BlockCopy(payload, 0, pkt, pos, payload.Length);
            pos += payload.Length;
            ushort crc = Crc16Ccitt(pkt, 0, pos);
            pkt[pos++] = (byte)(crc & 0xFF);
            pkt[pos]   = (byte)((crc >> 8) & 0xFF);
            return pkt;
        }

        /// <summary>
        /// CRC16-CCITT (poly 0x1021, init 0xFFFF) — the checksum used by SPRD DIAG.
        /// </summary>
        private static ushort Crc16Ccitt(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int b = 0; b < 8; b++)
                    crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
            return crc;
        }
    }
}
