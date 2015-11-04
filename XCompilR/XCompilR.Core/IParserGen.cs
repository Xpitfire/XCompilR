using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCompilR.Core
{
    public interface IParserGen
    {
        AParser CreateParser(string grammarFile);
    }
}
