using System;
using System.IO;

namespace iReverse_Unisoc_Ultimate.UniFlash.Validation
{
    internal static class FdlValidator
    {
        public const int MinimumFdlSize = 1024;

        public static ValidationResult Validate(string filePath)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.IsValid = false;
                result.ErrorMessage = "FDL file path is empty.";
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.ErrorMessage = $"FDL file not found: {filePath}";
                return result;
            }

            FileInfo info = new FileInfo(filePath);

            if (info.Length < MinimumFdlSize)
            {
                result.IsValid = false;
                result.ErrorMessage = $"FDL file too small: {info.Length} bytes (minimum {MinimumFdlSize} bytes).";
                return result;
            }

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    byte[] header = reader.ReadBytes(4);

                    if (header.Length >= 1 && header[0] == 0x7E)
                    {
                        result.IsValid = true;
                        result.Warning = "FDL header starts with 0x7E (diagnostic mode frame).";
                        return result;
                    }

                    if (header.Length >= 4)
                    {
                        uint magic = BitConverter.ToUInt32(header, 0);
                        if (magic == 0x0A0A0A0A || magic == 0xFFFFFFFF || magic == 0x564E5431)
                        {
                            result.IsValid = true;
                            result.Warning = "FDL header contains known magic pattern.";
                            return result;
                        }
                    }

                    result.IsValid = true;
                    result.Warning = "FDL header format unrecognized, but file exists and has valid size.";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Error reading FDL file: {ex.Message}";
                return result;
            }

            return result;
        }

        public static bool IsValid(string filePath)
        {
            return Validate(filePath).IsValid;
        }
    }

    internal class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string Warning { get; set; }
    }
}
