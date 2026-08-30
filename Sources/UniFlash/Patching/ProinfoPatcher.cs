using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace iReverse_Unisoc_Ultimate.UniFlash.Patching
{
    internal class ProinfoPatcher
    {
        public class PatchResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int PatternsRemoved { get; set; }
            public int BytesModified { get; set; }
            public List<string> Details { get; set; } = new List<string>();
        }

        public static PatchResult Patch(string inputPath, string outputPath)
        {
            var result = new PatchResult();

            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                result.Success = false;
                result.Message = "Input file not found: " + inputPath;
                return result;
            }

            try
            {
                byte[] data = File.ReadAllBytes(inputPath);
                int originalLength = data.Length;
                var details = new List<string>();

                // ── Pattern A: MDM / Security flags in proinfo ──────────────
                string[] mdmFlags = new string[]
                {
                    "MDM_ENABLED",
                    "MDM_LOCKED",
                    "SECURITY_LOCK",
                    "SECURITY_LOCKED",
                    "ANTI_CRACK",
                    "ANTICRACK",
                    "TRIGGER_P7",
                    "TRIGGERP7",
                    "P7_LOCK",
                    "SECURELOCK",
                    "SECURE_LOCK",
                    "DEVICE_LOCKED",
                    "DEVICE_LOCK",
                    "FLASH_LOCK",
                    "FLASH_LOCKED",
                    "BOOT_LOCK",
                    "BOOT_LOCKED",
                    "SECURITY_PLUGIN",
                    "SECURITYPLUGIN",
                    "DEVICE_ADMIN",
                    "DEVICEADMIN",
                    "TRANS_MDM",
                    "TRANS_SECURITY",
                    "ITEL_MDM",
                    "INFINIX_MDM",
                    "TECNO_MDM",
                    "HIOS_SECURITY",
                    "XOS_SECURITY",
                    "OS_LOCK",
                    "OS_LOCKED",
                    "SYS_LOCK",
                    "SYS_LOCKED",
                    "PROINFO_LOCK",
                    "PROINFO_LOCKED",
                    "NVM_LOCK",
                    "NVM_LOCKED"
                };

                int flagsRemoved = 0;
                foreach (string flag in mdmFlags)
                {
                    byte[] flagBytes = Encoding.ASCII.GetBytes(flag);
                    int pos = FindPattern(data, flagBytes);
                    if (pos >= 0)
                    {
                        int clearLen = flagBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        flagsRemoved++;
                        details.Add("Removed flag: " + flag + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern B: Security NV IDs in proinfo ───────────────────
                ushort[] securityNvIds = new ushort[]
                {
                    0x22F, 0x230, 0x4D2, 0x1F3, 0x7E5,
                    0x1A3, 0x1A4, 0x1F0, 0x1F1, 0x1F2, 0x1F4, 0x7E4,
                    0x5, 0x179, 0x17A, 0x17B, 0x17C, 0x17D
                };

                int nvIdsRemoved = 0;
                foreach (ushort nvId in securityNvIds)
                {
                    byte[] nvIdBytes = BitConverter.GetBytes(nvId);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(nvIdBytes);

                    int pos = FindPattern(data, nvIdBytes);
                    if (pos >= 0)
                    {
                        for (int i = 0; i < nvIdBytes.Length; i++)
                            data[pos + i] = 0x00;
                        nvIdsRemoved++;
                        details.Add("Zeroed NV ID 0x" + nvId.ToString("X") + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern C: DIAG command bytes ───────────────────────────
                int diagRemoved = 0;
                byte[] diagNvWrite = new byte[] { 0x7B };
                byte[] diagOem = new byte[] { 0x89 };

                int pos7B = FindPattern(data, diagNvWrite);
                if (pos7B >= 0)
                {
                    int clearLen = Math.Min(256, data.Length - pos7B);
                    for (int i = 0; i < clearLen; i++)
                        data[pos7B + i] = 0x00;
                    diagRemoved++;
                    details.Add("Zeroed DIAG NV-write packet at 0x" + pos7B.ToString("X"));
                }

                int pos89 = FindPattern(data, diagOem);
                if (pos89 >= 0)
                {
                    int clearLen = Math.Min(256, data.Length - pos89);
                    for (int i = 0; i < clearLen; i++)
                        data[pos89 + i] = 0x00;
                    diagRemoved++;
                    details.Add("Zeroed DIAG OEM packet at 0x" + pos89.ToString("X"));
                }

                // ── Pattern D: ATS / Transsion lock sector markers ──────────
                int atsRemoved = 0;
                byte[] atsMarker = Encoding.ASCII.GetBytes("ATS");
                byte[] transsionMarker = Encoding.ASCII.GetBytes("TRANS");
                byte[] itelMarker = Encoding.ASCII.GetBytes("ITEL");
                byte[] infinixMarker = Encoding.ASCII.GetBytes("INFINIX");
                byte[] tecnoMarker = Encoding.ASCII.GetBytes("TECNO");

                int atsPos = FindPattern(data, atsMarker);
                if (atsPos >= 0)
                {
                    int clearLen = Math.Min(64, data.Length - atsPos);
                    for (int i = 0; i < clearLen; i++)
                        data[atsPos + i] = 0x00;
                    atsRemoved++;
                    details.Add("Zeroed ATS marker at 0x" + atsPos.ToString("X"));
                }

                int transPos = FindPattern(data, transsionMarker);
                if (transPos >= 0)
                {
                    int clearLen = Math.Min(64, data.Length - transPos);
                    for (int i = 0; i < clearLen; i++)
                        data[transPos + i] = 0x00;
                    atsRemoved++;
                    details.Add("Zeroed TRANS marker at 0x" + transPos.ToString("X"));
                }

                int itelPos = FindPattern(data, itelMarker);
                if (itelPos >= 0)
                {
                    int clearLen = Math.Min(64, data.Length - itelPos);
                    for (int i = 0; i < clearLen; i++)
                        data[itelPos + i] = 0x00;
                    atsRemoved++;
                    details.Add("Zeroed ITEL marker at 0x" + itelPos.ToString("X"));
                }

                int infinixPos = FindPattern(data, infinixMarker);
                if (infinixPos >= 0)
                {
                    int clearLen = Math.Min(64, data.Length - infinixPos);
                    for (int i = 0; i < clearLen; i++)
                        data[infinixPos + i] = 0x00;
                    atsRemoved++;
                    details.Add("Zeroed INFINIX marker at 0x" + infinixPos.ToString("X"));
                }

                int tecnoPos = FindPattern(data, tecnoMarker);
                if (tecnoPos >= 0)
                {
                    int clearLen = Math.Min(64, data.Length - tecnoPos);
                    for (int i = 0; i < clearLen; i++)
                        data[tecnoPos + i] = 0x00;
                    atsRemoved++;
                    details.Add("Zeroed TECNO marker at 0x" + tecnoPos.ToString("X"));
                }

                // ── Write output ─────────────────────────────────────────────
                string outDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllBytes(outputPath, data);

                result.Success = true;
                result.PatternsRemoved = flagsRemoved + nvIdsRemoved + diagRemoved + atsRemoved;
                result.BytesModified = Math.Abs(data.Length - originalLength);
                result.Details = details;
                result.Message = "Proinfo patch applied. " + result.PatternsRemoved + " patterns removed/modified.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Error: " + ex.Message;
            }

            return result;
        }

        private static int FindPattern(byte[] data, byte[] pattern)
        {
            if (pattern.Length == 0 || data.Length < pattern.Length)
                return -1;

            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            return -1;
        }
    }
}
