using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pseudo.Net.AbstractSyntaxTree;

namespace XCompilR.Core
{
    internal delegate void BindDynamicObject(string ident, string value);

    public abstract class AParser
    {
        public dynamic BindingObject { get; set; }
        public ProgramRootNode ProgramRoot { get; set; }

        public abstract void InitParser(AScanner scanner);
        public abstract void ReInitParser();
        public abstract void Parse(string fileName);

    }
}
