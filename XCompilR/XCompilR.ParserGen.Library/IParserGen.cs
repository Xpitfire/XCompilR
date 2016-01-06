using System.Reflection;
using PostSharp.Extensibility;
using XCompilR.Library;

namespace XCompilR.ParserGen.Library
{
    public interface IParserGen
    {
        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        AParser CreateParser(string grammarFile);
        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        Assembly GenerateAssembly(string grammarFile);
    }
}
