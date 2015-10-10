using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;

namespace VeApps.Experiments
{
    class Program
    {
        static void Main(string[] args)
        {
            Antlr4.Runtime.Parser p = new ParserInterpreter("ECMAScript.g4", new List<string>(), new List<string>(), new ATN(ATNType.Lexer, 100), null);
            Console.WriteLine(p.ToString());
        }
    }
}
