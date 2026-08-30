using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace iReverse_Unisoc_Ultimate.UniFlash.Patching
{
    internal class BootloaderPatcher
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

                // ── Pattern A: Brand/Model strings ───────────────────────────
                string[] brandStrings = new string[]
                {
                    "TRANS",
                    "ITEL",
                    "INFINIX",
                    "TECNO",
                    "A669L",
                    "SQ375",
                    "UGO",
                    "OP",
                    "SPRD",
                    "UNISOC",
                    "C_TYPE"
                };

                int stringsRemoved = 0;
                foreach (string search in brandStrings)
                {
                    byte[] searchBytes = Encoding.ASCII.GetBytes(search);
                    int pos = FindPattern(data, searchBytes);
                    if (pos >= 0)
                    {
                        int clearLen = searchBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        stringsRemoved++;
                        details.Add("Zeroed brand string: " + search + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern B: Bootloader anti-tamper / signature checks ─────
                string[] bootloaderStrings = new string[]
                {
                    "ANTI_CRACK",
                    "ANTICRACK",
                    "TRIGGER_P7",
                    "TRIGGERP7",
                    "SECURITY_LOCK",
                    "SECURE_BOOT",
                    "SECUREBOOT",
                    "LOCK_STATE",
                    "LOCKSTATE",
                    "GETLOCKSTATE",
                    "SET_SECURITY_FLAG",
                    "SECURELOCK",
                    "SPCLSIMLOCK",
                    "SPFRPRESET",
                    "CLRFRP",
                    "CLRANTICRACK",
                    "SPTEST",
                    "SPFACTORY",
                    "PHASE_CHECK",
                    "PHASECHECK",
                    "ATS_LOCK",
                    "ATSLOCK",
                    "CUSTOMER_LOCK",
                    "CUSTOMERLOCK",
                    "FACTORY_LOCK",
                    "FACTORYLOCK",
                    "PRODUCTION_LOCK",
                    "PRODUCTIONLOCK",
                    "ENG_MODE",
                    "ENGMODE",
                    "FACTORY_MODE",
                    "FACTORYMODE"
                };

                foreach (string search in bootloaderStrings)
                {
                    byte[] searchBytes = Encoding.ASCII.GetBytes(search);
                    int pos = FindPattern(data, searchBytes);
                    if (pos >= 0)
                    {
                        int clearLen = searchBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        stringsRemoved++;
                        details.Add("Zeroed bootloader string: " + search + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern C: NV IDs in bootloader ──────────────────────────
                ushort[] antiCrackNvIds = new ushort[]
                {
                    0x22F, 0x230, 0x4D2, 0x1F3, 0x7E5,
                    0x1A3, 0x1A4, 0x1F0, 0x1F1, 0x1F2, 0x1F4, 0x7E4,
                    0x5, 0x179
                };

                int nvIdsRemoved = 0;
                foreach (ushort nvId in antiCrackNvIds)
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

                // ── Pattern D: DIAG command bytes ────────────────────────────
                int diagPacketsRemoved = 0;
                byte[] diagNvWrite = new byte[] { 0x7B };
                byte[] diagOem = new byte[] { 0x89 };

                int pos7B = FindPattern(data, diagNvWrite);
                if (pos7B >= 0)
                {
                    int clearLen = Math.Min(512, data.Length - pos7B);
                    for (int i = 0; i < clearLen; i++)
                        data[pos7B + i] = 0x00;
                    diagPacketsRemoved++;
                    details.Add("Zeroed DIAG NV-write packet at 0x" + pos7B.ToString("X"));
                }

                int pos89 = FindPattern(data, diagOem);
                if (pos89 >= 0)
                {
                    int clearLen = Math.Min(512, data.Length - pos89);
                    for (int i = 0; i < clearLen; i++)
                        data[pos89 + i] = 0x00;
                    diagPacketsRemoved++;
                    details.Add("Zeroed DIAG OEM packet at 0x" + pos89.ToString("X"));
                }

                // ── Write output ─────────────────────────────────────────────
                string outDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllBytes(outputPath, data);

                result.Success = true;
                result.PatternsRemoved = stringsRemoved + nvIdsRemoved + diagPacketsRemoved;
                result.BytesModified = Math.Abs(data.Length - originalLength);
                result.Details = details;
                result.Message = "Bootloader patch applied. " + result.PatternsRemoved + " patterns removed/modified.";
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
