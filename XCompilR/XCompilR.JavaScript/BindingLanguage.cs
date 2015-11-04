using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCompilR.Core;

namespace XCompilR.JavaScript
{
    public class BindingLanguage : ABindingLanguage
    {
        public override string LanguageName { get; } = "JavaScript";
        public override string AssemblyName { get; } = "XCompilR.JavaScript";
        public override string ParserTypeName { get; } = "XCompilR.JavaScript.Parser";
        public override string ScannerTypeName { get; } = "XCompilR.JavaScript.Scanner";
    }
}
