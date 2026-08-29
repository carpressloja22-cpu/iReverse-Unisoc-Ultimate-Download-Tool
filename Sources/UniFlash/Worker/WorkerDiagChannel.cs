using iReverse_Unisoc_Ultimate.MyUI;
using iReverse_Unisoc_Ultimate.Utility.Connection;
using iReverse_Unisoc_Ultimate.Utility.Connection.API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using static iReverse_Unisoc_Ultimate.Utility.Connection.PortIO;
using static iReverse_Unisoc_Ultimate.Utility.Connection.USBFastConnect;

namespace iReverse_Unisoc_Ultimate
{
    namespace UniFlash.Worker
    {
        internal static class WorkerDiagChannel
        {
            public static bool busyState = false;
            public static byte[] DiagChannelPayload = uni.StringToByteArray("7E 00 00 00 00 08 00 FE 81 7E");

            private static bool IsCancelled(DoWorkEventArgs e)
            {
                if (!Main.SharedUI.UnisocWorker.CancellationPending)
                    return false;

                e.Cancel = true;
                return true;
            }

            private static void SetDiagConnected(bool value)
            {
                if (Main.SharedUI == null || Main.SharedUI.CkDiagConnected == null)
                    return;

                Main.SharedUI.CkDiagConnected.Invoke(
                    (Action)(() => Main.SharedUI.CkDiagConnected.Checked = value)
                );
            }

            private static void Log(string text, Color color, bool newline)
            {
                MyDisplay.RichLogs(text, color, true, newline);
            }

            private static bool EnsureConnection(object sender, DoWorkEventArgs e)
            {
                if (IsCancelled(e))
                    return false;

                if (!DiagChannelOpenPort(sender, e))
                    return false;

                if (IsCancelled(e))
                    return false;

                if (string.IsNullOrEmpty(WorkerGlobal.PortCom))
                {
                    if (!MyDisplay.USBSearchPort())
                    {
                        Log("Diag device not found.", Color.Red, true);
                        return false;
                    }
                }

                // Attempt Diag connect with retry
                DiagResult result = DiagService.Connect(WorkerGlobal.PortCom);
                if (!result.Success)
                {
                    Thread.Sleep(1000);
                    result = DiagService.Connect(WorkerGlobal.PortCom);
                }

                if (!result.Success)
                {
                    Log("Diag connection failed: " + result.Message, Color.Red, true);
                    return false;
                }

                SetDiagConnected(true);
                Log("Diag channel connected.", Color.Purple, true);
                return true;
            }

            public static void UniWorkerDiagChannel(object sender, DoWorkEventArgs e)
            {
                busyState = true;
                try
                {
                    Log("Operation\t: ", Color.Black, false);
                    Log(MyDisplay.MyOperation, Color.Purple, true);

                    if (!EnsureConnection(sender, e))
                        return;

                    if (IsCancelled(e))
                        return;

                    LogDiagContext(e);
                    if (IsCancelled(e))
                        return;

                    switch (WorkerGlobal.WorkerMethod)
                    {
                        case "Factory Reset":
                            ExecuteFactoryReset(e);
                            break;

                        case "Power Off":
                            ExecutePowerOff(e);
                            break;

                        case "Send ATCommand":
                            ExecuteSendAt(e);
                            break;

                        case "Read IMEI":
                            ExecuteReadImei(e);
                            break;

                        case "Write IMEI 1":
                            ExecuteWriteImei(e, "1", Main.SharedUI.TxtIMEI1.Text);
                            break;

                        case "Write IMEI 2":
                            ExecuteWriteImei(e, "2", Main.SharedUI.TxtIMEI2.Text);
                            break;

                        case "Remove Anti-Crack":
                            ExecuteRemoveAntiCrack(e);
                            break;

                        case "Enter Diag Mode":
                            Log("Diag mode is already active.", Color.Purple, true);
                            MyProgress.ProcessBar1(100);
                            break;

                        default:
                            Log("Unsupported Diag operation: " + WorkerGlobal.WorkerMethod, Color.Red, true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log("Diag error: " + ex.Message, Color.Red, true);
                    Console.WriteLine(ex);
                }
                finally
                {
                    DiagChannel.DiagClose();
                    try { PortClose(); } catch (Exception closeEx) { Console.WriteLine("PortClose: " + closeEx.Message); }
                    SetDiagConnected(false);
                    busyState = false;
                }
            }

            private static void LogDiagContext(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                string version;
                DiagResult result = DiagService.ReadApVersion(out version);
                if (result.Success && !string.IsNullOrEmpty(version))
                    Log("SW Info\t: " + version, Color.Black, true);
                else
                    Log("SW Info\t: unavailable", Color.Black, true);
            }

            private static void ExecuteFactoryReset(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                MyProgress.ProcessBar1(25);
                string response;
                DiagResult result = DiagService.SendAt(
                    "AT+SPDIAG=\"AT+ETSRESET\"",
                    out response
                );

                Log(result.Success ? "Factory reset command accepted." : "Factory reset command failed: " + result.Message,
                    result.Success ? Color.Purple : Color.Red, true);
                if (!string.IsNullOrEmpty(response))
                    Log("Response: " + response, Color.Black, true);

                MyProgress.ProcessBar1(100);
            }

            private static void ExecutePowerOff(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                MyProgress.ProcessBar1(25);
                int result = PhoneCommandAPI.SP_PowerOff(DiagChannel.hDiagPhone);
                if (result == 0)
                    Log("Power off command accepted.", Color.Purple, true);
                else
                    Log("Power off failed, error " + result, Color.Red, true);
                MyProgress.ProcessBar1(100);
            }

            private static void ExecuteSendAt(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                string command = Main.SharedUI.TxtATCommand.Text;
                MyProgress.ProcessBar1(25);
                string response;
                DiagResult result = DiagService.SendAt(command, out response);

                Log(result.Success ? "OK" : "FAIL, Error " + result.NativeResult,
                    result.Success ? Color.Purple : Color.Red, true);
                if (!string.IsNullOrEmpty(response))
                    Log("Response: " + response, Color.Black, true);
                MyProgress.ProcessBar1(100);
            }

            private static void ExecuteReadImei(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                string imei1;
                string imei2;
                DiagResult r1 = DiagService.ReadImei(PhoneCommandAPI.NVID_IMEI1, out imei1);
                MyProgress.ProcessBar1(25);
                DiagResult r2 = DiagService.ReadImei(PhoneCommandAPI.NVID_IMEI2, out imei2);

                if (r1.Success)
                    Main.SharedUI.TxtIMEI1.Invoke((Action)(() => Main.SharedUI.TxtIMEI1.Text = imei1));
                if (r2.Success)
                    Main.SharedUI.TxtIMEI2.Invoke((Action)(() => Main.SharedUI.TxtIMEI2.Text = imei2));

                Log("IMEI 1: " + (r1.Success ? imei1 : "FAIL " + r1.NativeResult),
                    r1.Success ? Color.Black : Color.Red, true);
                Log("IMEI 2: " + (r2.Success ? imei2 : "FAIL " + r2.NativeResult),
                    r2.Success ? Color.Black : Color.Red, true);
                MyProgress.ProcessBar1(100);
            }

            private static void ExecuteWriteImei(DoWorkEventArgs e, string number, string imei)
            {
                if (IsCancelled(e)) return;

                ushort nvId;
                if (number == "1")
                    nvId = PhoneCommandAPI.NVID_IMEI1;
                else if (number == "2")
                    nvId = PhoneCommandAPI.NVID_IMEI2;
                else
                {
                    Log("Invalid IMEI number.", Color.Red, true);
                    return;
                }

                if (string.IsNullOrWhiteSpace(imei) || imei.Trim().Length != 15)
                {
                    Log("Invalid IMEI " + number + ". Expected 15 digits.", Color.Red, true);
                    return;
                }

                MyProgress.ProcessBar1(25);
                Log("WRITE IMEI " + number + " : " + imei + "... ", Color.Black, false);
                int result = PhoneCommandAPI.SP_WriteImei(DiagChannel.hDiagPhone, nvId, imei.Trim());
                if (result == 0)
                    Log("OK", Color.Purple, true);
                else
                    Log("FAIL, Error " + result, Color.Red, true);
                MyProgress.ProcessBar1(100);
            }

            private static void ExecuteRemoveAntiCrack(DoWorkEventArgs e)
            {
                if (IsCancelled(e)) return;

                Log("--- [Starting Anti-Crack / Trigger P7 Removal] ---", Color.Purple, true);
                MyProgress.ProcessBar1(5);

                // ── Detect lock state ─────────────────────────────────────────
                string lockStatus = string.Empty;
                Log("1. Reading Lock State... ", Color.Black, false);
                DiagResult rStatus = DiagService.SendAt("AT+SPDIAG=\"AT+GETLOCKSTATE\"", out lockStatus);
                if (!rStatus.Success || string.IsNullOrEmpty(lockStatus))
                {
                    DiagService.SendAt("AT+SPDIAG=\"AT+ANTICRACK?\"", out lockStatus);
                }
                Log("OK", Color.Purple, true);
                if (!string.IsNullOrEmpty(lockStatus))
                    Log("   Current State: " + lockStatus, Color.Black, true);
                MyProgress.ProcessBar1(10);

                if (IsCancelled(e)) return;

                // ── Method A: AT command sequences ────────────────────────────
                bool methodASuccess = false;
                Log("2A. Trying AT command sequences... ", Color.Black, false);
                string response;
                string[] atCommands = new string[]
                {
                    "AT+SPDIAG=\"AT+ANTICRACK=0\"",
                    "AT+SPDIAG=\"AT+CLRANTICRACK\"",
                    "AT+SPDIAG=\"AT+CLRFRP\"",
                    "AT+SPDIAG=\"AT+SPFRPRESET\"",
                    "AT+SPDIAG=\"AT+SPCLSIMLOCK\"",
                    "AT+SPDIAG=\"AT+SECURELOCK=0\"",
                    "AT+SPDIAG=\"AT+SET_SECURITY_FLAG=0\"",
                    "AT+SPDIAG=\"AT+SPTEST=1\"",
                    "AT+SPDIAG=\"AT+SPFACTORY\"",
                    "AT+SPDIAG=\"AT+ETSRESET\""
                };

                foreach (string cmd in atCommands)
                {
                    if (IsCancelled(e)) return;
                    DiagResult r = DiagService.SendAt(cmd, out response);
                    Log("   " + cmd.Substring(cmd.Length - 20) + " => " + (r.Success ? "OK" : "FAIL"), Color.Black, true);
                    if (r.Success) methodASuccess = true;
                }
                Log("Method A: " + (methodASuccess ? "Partial/Full success" : "No response"), Color.Purple, true);
                MyProgress.ProcessBar1(30);

                if (IsCancelled(e)) return;

                // ── Method B: Raw DIAG NV-write packets ───────────────────────
                bool methodBSuccess = false;
                Log("2B. Trying raw DIAG NV-write packets... ", Color.Black, false);
                ushort[] antiCrackNvIds = new ushort[]
                {
                    PhoneCommandAPI.NVID_ANTI_CRACK_FLAG,
                    PhoneCommandAPI.NVID_SECURITY_STATE,
                    PhoneCommandAPI.NVID_SIM_LOCK_EX_DATA,
                    PhoneCommandAPI.NVID_SIM_CFG2,
                    PhoneCommandAPI.NVID_ANTI_CRACK_EXT,
                    PhoneCommandAPI.NVID_SIMLOCK_SIGN,
                    PhoneCommandAPI.NVID_SIMLOCK_DATA,
                    PhoneCommandAPI.NVID_SIM_LOCK_CUSTOMIZE_DATA,
                    PhoneCommandAPI.NVID_SIM_LOCK_USER_DATA,
                    PhoneCommandAPI.NVID_SIM_LOCK_CONTROL_KEY,
                    PhoneCommandAPI.NVID_SIM_LOCK_STORAGE_KEY,
                    PhoneCommandAPI.NVID_NV_PARAM_TYPE_SIM_CFG1
                };

                foreach (ushort nvId in antiCrackNvIds)
                {
                    if (IsCancelled(e)) return;
                    byte[] zeroData = new byte[128];
                    DiagResult r = DiagService.WriteNV(nvId, zeroData);
                    Log("   NV 0x" + nvId.ToString("X") + " => " + (r.Success ? "OK" : "FAIL"), Color.Black, true);
                    if (r.Success) methodBSuccess = true;
                }
                Log("Method B: " + (methodBSuccess ? "Partial/Full success" : "No response"), Color.Purple, true);
                MyProgress.ProcessBar1(50);

                if (IsCancelled(e)) return;

                // ── Method C: OEM DIAG phase-clear packets ────────────────────
                bool methodCSuccess = false;
                Log("2C. Trying OEM DIAG phase-clear packets... ", Color.Black, false);
                byte[] phaseClrPacket = DiagService.BuildSprdOemPacket(0x89, new byte[] { 0x03, 0x00, 0x00, 0x00 });
                byte[] phaseClrAlt = DiagService.BuildSprdOemPacket(0x89, new byte[] { 0x06, 0x00, 0x00, 0x00 });
                byte[] reply;
                DiagResult rPhase = DiagService.SendRawDiagPacket(phaseClrPacket, out reply);
                Log("   OEM 0x89/0x03 => " + (rPhase.Success ? "OK" : "FAIL"), Color.Black, true);
                if (rPhase.Success) methodCSuccess = true;

                DiagResult rPhaseAlt = DiagService.SendRawDiagPacket(phaseClrAlt, out reply);
                Log("   OEM 0x89/0x06 => " + (rPhaseAlt.Success ? "OK" : "FAIL"), Color.Black, true);
                if (rPhaseAlt.Success) methodCSuccess = true;
                Log("Method C: " + (methodCSuccess ? "Partial/Full success" : "No response"), Color.Purple, true);
                MyProgress.ProcessBar1(65);

                if (IsCancelled(e)) return;

                // ── Method D: Customer factory reset via DIAG ─────────────────
                bool methodDSuccess = false;
                Log("2D. Trying customer factory reset... ", Color.Black, false);
                DiagResult rCustomer = DiagService.CustomerPhoneReset();
                Log("   SP_CustomerPhoneOp(FACTORY_RESET) => " + (rCustomer.Success ? "OK" : "FAIL"), Color.Black, true);
                if (rCustomer.Success) methodDSuccess = true;
                Log("Method D: " + (methodDSuccess ? "Success" : "No response"), Color.Purple, true);
                MyProgress.ProcessBar1(75);

                // ── Verification: read back NV items ─────────────────────────
                Log("3. Verifying NV items... ", Color.Black, false);
                bool verified = true;
                foreach (ushort nvId in antiCrackNvIds)
                {
                    if (IsCancelled(e)) return;
                    byte[] data;
                    DiagResult rRead = DiagService.ReadNV(nvId, out data);
                    bool isZero = data != null && data.Length > 0 && Array.TrueForAll(data, b => b == 0);
                    Log("   NV 0x" + nvId.ToString("X") + " => " + (isZero ? "CLEARED" : "NOT CLEARED"), isZero ? Color.Purple : Color.Orange, true);
                    if (!isZero) verified = false;
                }
                Log("Verification: " + (verified ? "All NV items cleared" : "Some NV items remain"), verified ? Color.Purple : Color.Orange, true);
                MyProgress.ProcessBar1(85);

                if (IsCancelled(e)) return;

                // ── Final reboot ─────────────────────────────────────────────
                Log("4. Rebooting device to normal mode... ", Color.Black, false);
                DiagResult rRestart = DiagService.RestartPhone(PhoneCommandAPI.RM_MODE_ENUM.RM_NORMAL_MODE);
                if (!rRestart.Success)
                {
                    DiagService.SendAt("AT+SPDIAG=\"AT+CFUN=1,1\"", out response);
                }
                Log("OK", Color.Purple, true);
                MyProgress.ProcessBar1(100);

                Log(" ", Color.Black, true);
                bool anySuccess = methodASuccess || methodBSuccess || methodCSuccess || methodDSuccess;
                if (anySuccess)
                {
                    Log("Anti-Crack / Trigger P7 removal attempted with multiple methods.", Color.Purple, true);
                    Log("If lock persists, the protection may be in bootloader/miscdata partition.", Color.Orange, true);
                    Log("Try flashing a clean firmware or using Download Mode erase.", Color.Black, true);
                }
                else
                {
                    Log("Anti-Crack removal failed — no method responded.", Color.Red, true);
                    Log("This firmware variant may not support these commands.", Color.Red, true);
                }
                Log("Device is rebooting. Please wait for normal boot.", Color.Black, true);
            }

            public static bool DiagChannelOpenPort(object sender, DoWorkEventArgs e)
            {
                bool iscontinue = true;
                busyState = true;

                // 1. Check if an SPRD/Diag device is already present in current devices
                List<comInfo> deviceList = UsbDeviceCache.GetDevices();
                comInfo selectedDevice = null;

                if (deviceList != null && deviceList.Count > 0)
                {
                    foreach (var d in deviceList)
                    {
                        if (d.name != null && (d.name.ToUpper().Contains("SPRD") || d.name.ToUpper().Contains("DIAG") || d.name.ToUpper().Contains("U2S")))
                        {
                            selectedDevice = d;
                            break;
                        }
                    }
                }

                if (selectedDevice == null)
                {
                    Log("Please connect 'usb' cable w/o pressing any boot button!", Color.Black, true);
                    Log("Waiting for U2S connection... ", Color.Black, false);

                    selectedDevice = FindNewDevice(deviceList ?? new List<comInfo>());

                    if (selectedDevice == null)
                    {
                        Log("Not Found!", Color.Red, true);
                        busyState = false;
                        return false;
                    }

                    Log("OK", Color.Purple, true);
                }
                else
                {
                    Log("Device detected\t: " + selectedDevice.name, Color.Purple, true);
                }

                string[] usb = VID_PID(selectedDevice.hwid);
                Log("Port Number\t\t: COM" + selectedDevice.comport, Color.Black, true);
                if (usb != null && usb.Length >= 2)
                {
                    Log("Vendor ID\t\t: " + usb[0], Color.Black, true);
                    Log("Product ID\t: " + usb[1], Color.Black, true);
                }

                Log("Handshaking... ", Color.Black, false);
                PortOpen(selectedDevice.comport);

                if (!serialPort.IsOpen)
                {
                    Log("Fail", Color.Red, true);
                    busyState = false;
                    return false;
                }

                if (IsCancelled(e))
                {
                    PortClose();
                    busyState = false;
                    return false;
                }

                Log("OK", Color.Purple, true);
                Log("Execute command... ", Color.Black, false);
                PortWrite(DiagChannelPayload);
                Log("OK", Color.Purple, true);
                Log(" ", Color.Purple, true);

                // CRITICAL: Close the serial port so PhoneCommand.dll can open COM port exclusively
                PortClose();
                Thread.Sleep(800);

                WorkerGlobal.PortCom = selectedDevice.comport;
                busyState = false;

                if (IsCancelled(e))
                {
                    e.Cancel = true;
                    return false;
                }

                return iscontinue;
            }
        }
    }
}
