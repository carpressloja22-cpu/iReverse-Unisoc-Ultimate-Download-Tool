using System;

namespace iReverse_Unisoc_Ultimate.UniFlash.Utility.Diag
{
    internal sealed class DiagResult
    {
        public bool Success { get; private set; }
        public bool Cancelled { get; private set; }
        public int NativeResult { get; private set; }
        public string Operation { get; private set; }
        public string Message { get; private set; }

        private DiagResult() { }

        public static DiagResult Ok(string operation, string message)
        {
            return new DiagResult
            {
                Success = true,
                Operation = operation,
                Message = message,
                NativeResult = 0
            };
        }

        public static DiagResult Fail(string operation, int nativeResult, string message)
        {
            return new DiagResult
            {
                Success = false,
                Operation = operation,
                Message = message,
                NativeResult = nativeResult
            };
        }

        public static DiagResult Cancel(string operation)
        {
            return new DiagResult
            {
                Success = false,
                Cancelled = true,
                Operation = operation,
                Message = "Operação cancelada."
            };
        }
    }
}
