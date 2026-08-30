using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace iReverse_Unisoc_Ultimate.UniFlash.Patching
{
    internal class SuperPatcher
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

                // ── Pattern A: SecurityPlugin / MDM package names ───────────
                string[] securityPackages = new string[]
                {
                    "com.transsion.securityplugin",
                    "com.transsion.security",
                    "com.transsion.mdm",
                    "com.itel.security",
                    "com.infinix.security",
                    "com.tecno.security",
                    "com.transsion.deviceadmin",
                    "com.transsion.antitheft",
                    "com.transsion.knox",
                    "com.transsion.securefolder",
                    "com.transsion.sfind",
                    "com.transsion.xtra",
                    "com.transsion.safetynet",
                    "com.transsion.attestation",
                    "com.transsion.verify",
                    "com.transsion.lock"
                };

                int packagesRemoved = 0;
                foreach (string pkg in securityPackages)
                {
                    byte[] pkgBytes = Encoding.ASCII.GetBytes(pkg);
                    int pos = FindPattern(data, pkgBytes);
                    if (pos >= 0)
                    {
                        int clearLen = pkgBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        packagesRemoved++;
                        details.Add("Removed package: " + pkg + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern B: Device admin / MDM strings ───────────────────
                string[] mdmStrings = new string[]
                {
                    "SecurityPlugin",
                    "SECURITYPLUGIN",
                    "security_plugin",
                    "securityplugin",
                    "DeviceAdmin",
                    "DEVICE_ADMIN",
                    "device_admin",
                    "DeviceAdminReceiver",
                    "BIND_DEVICE_ADMIN",
                    "android.app.device_admin",
                    "com.android.settings.deviceadmin",
                    "MDM",
                    "mdm",
                    "MobileDeviceManagement",
                    "device_owner",
                    "profile_owner",
                    "TRANS_MDM",
                    "TRANS_SECURITY",
                    "ITEL_MDM",
                    "INFINIX_MDM",
                    "TECNO_MDM",
                    "HIOS_SECURITY",
                    "XOS_SECURITY",
                    "OneUI_Security"
                };

                int stringsRemoved = 0;
                foreach (string search in mdmStrings)
                {
                    byte[] searchBytes = Encoding.ASCII.GetBytes(search);
                    int pos = FindPattern(data, searchBytes);
                    if (pos >= 0)
                    {
                        int clearLen = searchBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        stringsRemoved++;
                        details.Add("Removed MDM string: " + search + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern C: Anti-tamper / signature verification ─────────
                string[] antiTamperStrings = new string[]
                {
                    "VERIFIED_BOOT",
                    "VERITY",
                    "dm_verity",
                    "avb",
                    "AVB",
                    "boot_verifier",
                    "boot_signature",
                    "system_verifier",
                    "ota_cert",
                    "ota_signature",
                    "ota_verify",
                    "SECURE_BOOT",
                    "secure_boot",
                    "LOCK_BOOTLOADER",
                    "LOCKED",
                    "locked",
                    "INTEGRITY",
                    "integrity",
                    "attestation",
                    "safetynet",
                    "SafetyNet",
                    "cts_profile",
                    "cts_profile_type",
                    "bootloader_locked",
                    "device_locked",
                    "flash_locked",
                    "anti_rollback",
                    "ANTI_ROLLBACK",
                    "rollback",
                    "ROLLBACK"
                };

                int antiTamperRemoved = 0;
                foreach (string search in antiTamperStrings)
                {
                    byte[] searchBytes = Encoding.ASCII.GetBytes(search);
                    int pos = FindPattern(data, searchBytes);
                    if (pos >= 0)
                    {
                        int clearLen = searchBytes.Length;
                        for (int i = 0; i < clearLen; i++)
                            data[pos + i] = 0x00;
                        antiTamperRemoved++;
                        details.Add("Removed anti-tamper: " + search + " at 0x" + pos.ToString("X"));
                    }
                }

                // ── Pattern D: DIAG/NV security markers ─────────────────────
                int diagRemoved = 0;
                byte[] diagNvWrite = new byte[] { 0x7B };
                byte[] diagOem = new byte[] { 0x89 };

                int pos7B = FindPattern(data, diagNvWrite);
                if (pos7B >= 0)
                {
                    int clearLen = Math.Min(512, data.Length - pos7B);
                    for (int i = 0; i < clearLen; i++)
                        data[pos7B + i] = 0x00;
                    diagRemoved++;
                    details.Add("Zeroed DIAG NV-write packet at 0x" + pos7B.ToString("X"));
                }

                int pos89 = FindPattern(data, diagOem);
                if (pos89 >= 0)
                {
                    int clearLen = Math.Min(512, data.Length - pos89);
                    for (int i = 0; i < clearLen; i++)
                        data[pos89 + i] = 0x00;
                    diagRemoved++;
                    details.Add("Zeroed DIAG OEM packet at 0x" + pos89.ToString("X"));
                }

                // ── Write output ─────────────────────────────────────────────
                string outDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllBytes(outputPath, data);

                result.Success = true;
                result.PatternsRemoved = packagesRemoved + stringsRemoved + antiTamperRemoved + diagRemoved;
                result.BytesModified = Math.Abs(data.Length - originalLength);
                result.Details = details;
                result.Message = "Super patch applied. " + result.PatternsRemoved + " patterns removed/modified.";
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
