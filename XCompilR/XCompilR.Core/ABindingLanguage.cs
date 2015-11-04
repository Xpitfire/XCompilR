using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Constraints;

namespace XCompilR.Core
{
    [Serializable]
    public abstract class ABindingLanguage
    {
        public abstract string LanguageName { get; }
        public abstract string AssemblyName { get; }
        public abstract string ParserTypeName { get; }
        public abstract string ScannerTypeName { get; }
    }
}
