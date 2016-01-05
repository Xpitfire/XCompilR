using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Extensibility;
using Roslyn.Compilers.CSharp;

namespace XCompilR.Core
{
    public abstract class AParser
    {
        public dynamic BindingObject { get; set; }

        public Dictionary<string, object> BindingObjectMembers { get; set; } 

        public CompilationUnitSyntax CompilationUnitSyntax { get; } = Syntax.CompilationUnit();

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void InitParser(AScanner scanner);

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void ReInitParser();

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void Parse(string fileName);
    }
}
