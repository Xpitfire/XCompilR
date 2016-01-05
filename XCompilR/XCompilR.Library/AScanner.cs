using System.IO;
using PostSharp.Extensibility;

namespace XCompilR.Library
{
    public abstract class AScanner
    {
        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract AScanner Scan(string fileName);

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract AScanner Scan(Stream stream);
    }
}
