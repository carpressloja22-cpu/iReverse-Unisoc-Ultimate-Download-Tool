using System;

namespace iReverse_Unisoc_Ultimate.Utility.Connection
{
    internal sealed class DiagResult
    {
        public bool Success { get; private set; }
        public bool Cancelled { get; private set; }
        public int NativeResult { get; private set; }
        public string Operation { get; private set; }
        public string Message { get; private set; }

        private DiagResult(bool success, bool cancelled, int nativeResult, string operation, string message)
        {
            Success = success;
            Cancelled = cancelled;
            NativeResult = nativeResult;
            Operation = operation ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public static DiagResult Ok(string operation, string message)
        {
            return new DiagResult(true, false, 0, operation, message);
        }

        public static DiagResult Fail(string operation, int nativeResult, string message)
        {
            return new DiagResult(false, false, nativeResult, operation, message);
        }

        public static DiagResult Cancel(string operation, string message)
        {
            return new DiagResult(false, true, 0, operation, message);
        }
    }
}
