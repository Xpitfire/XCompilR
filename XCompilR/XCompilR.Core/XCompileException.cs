using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    [Serializable]
    public class XCompileException : Exception
    {
        public XCompileException() { }

        public XCompileException(string message) : base(message) { }

        public XCompileException(string message, Exception innerException) : base(message, innerException) { }
    }
}
