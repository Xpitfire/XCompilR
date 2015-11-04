using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    internal delegate void BindDynamicObject(string ident, string value);

    public abstract class AParser
    {
        public abstract void InitParser(AScanner scanner);
        public abstract void ReInitParser();
        public abstract void Parse(string fileName);
    }
}
