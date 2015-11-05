using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCompilR.Core;
using XCompilR.PSEUDO;

namespace XCompilR.PseudoNet
{
    public class BindingLanguage : ABindingLanguage
    {
        public override string LanguageName { get; } = "PseudoNet";
        public override string AssemblyName { get; } = "XCompilR.PseudoNet";
        public override AParser Parser { get; }

        public BindingLanguage()
        {
            var p = new Parser();
            p.InitParser(new Scanner());
            Parser = p;
        }
    }
}
