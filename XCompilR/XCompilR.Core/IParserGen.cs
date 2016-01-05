using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Extensibility;

namespace XCompilR.Core
{
    public interface IParserGen
    {
        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        AParser CreateParser(string grammarFile);
    }
}
