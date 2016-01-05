using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Extensibility;

namespace XCompilR.Core
{
    public abstract class AScanner
    {
        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract AScanner Scan(string fileName);

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract AScanner Scan(Stream stream);
    }
}
