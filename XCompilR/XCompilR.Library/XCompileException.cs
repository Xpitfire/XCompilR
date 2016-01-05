using System;

namespace XCompilR.Library
{
    [Serializable]
    public class XCompileException : Exception
    {
        public XCompileException() { }

        public XCompileException(string message) : base(message) { }

        public XCompileException(string message, Exception innerException) : base(message, innerException) { }
    }
}
