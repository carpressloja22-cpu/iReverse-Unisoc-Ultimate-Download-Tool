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

                if (!MyDisplay.USBSearchPort())
                {
                    Log("Diag device not found.", Color.Red, true);
                    return false;
                }

                DiagResult result = DiagService.Connect(WorkerGlobal.PortCom);
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

            public static bool DiagChannelOpenPort(object sender, DoWorkEventArgs e)
            {
                bool iscontinue = true;
                Log("Please connect 'usb' cable w/o pressing any boot button!", Color.Black, true);
                Log("Waiting for U2S connection... ", Color.Black, false);

                busyState = true;
                List<comInfo> deviceList = UsbDeviceCache.GetDevices();
                comInfo selectedDevice = FindNewDevice(deviceList);

                if (selectedDevice == null)
                {
                    Log("Not Found!", Color.Red, true);
                    busyState = false;
                    return false;
                }

                Log("OK", Color.Purple, true);
                string[] usb = VID_PID(selectedDevice.hwid);
                Log("Port Number\t\t: COM" + selectedDevice.comport, Color.Black, true);
                Log("Vendor ID\t\t: " + usb[0], Color.Black, true);
                Log("Product ID\t: " + usb[1], Color.Black, true);

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
                    busyState = false;
                    return false;
                }

                Log("OK", Color.Purple, true);
                Log("Execute command... ", Color.Black, false);
                PortWrite(DiagChannelPayload);
                Log("OK", Color.Purple, true);
                Log(" ", Color.Purple, true);
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
