using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using at.jku.ssw.Coco;
using Microsoft.CSharp;
using PostSharp.Patterns.Diagnostics;
using XCompilR.Library;
using XCompilR.ParserGen.Library;

namespace XCompile.ParserGen.CocoR
{
    public class CocoParserGen : IParserGen
    {
        [LogException]
        private Assembly LoadAssembly(string srcName, string nsName)
        {
            try
            {
                string srcDir = Path.GetDirectoryName(srcName);
                string outDir = ".";

                Scanner scanner = new Scanner(srcName);
                Parser parser = new Parser(scanner);

                string traceFileName = Path.Combine(srcDir ?? "", "trace.txt");
                parser.trace = new StreamWriter(new FileStream(traceFileName, FileMode.Create));
                parser.tab = new Tab(parser);
                parser.dfa = new DFA(parser);
                parser.pgen = new at.jku.ssw.Coco.ParserGen(parser);

                parser.tab.srcName = srcName;
                parser.tab.srcDir = srcDir;
                parser.tab.nsName = nsName;
                parser.tab.outDir = outDir;

                parser.Parse();

                parser.trace.Close();

                if (parser.errors.count != 0)
                {
                    throw new InvalidOperationException($"Failed to parse grammar file! Found {parser.errors.count} errors.");
                }

                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true,
                    OutputAssembly = nsName,
                    TreatWarningsAsErrors = false
                };
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("mscorlib.dll");
                parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
                parameters.ReferencedAssemblies.Add("XCompilR.Library.dll");

                var codeProvider = new CSharpCodeProvider(new Dictionary<string, string>
                {
                    { "CompilerVersion", "v4.0" }
                });
                var results = codeProvider.CompileAssemblyFromFile(parameters, "Parser.cs", "Scanner.cs");

                if (results.Errors.HasErrors || results.Errors.HasWarnings)
                {
                    throw new InvalidOperationException($"Failed to generate dynamic assembly! Found {results.Errors.Count} errors.");
                }

                return results.CompiledAssembly;
            }
            catch (Exception exception)
            {
                throw new XCompileException($"Could not create {nsName} assembly!", exception);
            }
        }

        [LogException]
        public AParser CreateParser(string srcName)
        {
            try
            {
                string nsName = $"XCompilR.{srcName.Split('.')[0]}";
                Assembly assembly = LoadAssembly(srcName, nsName);

                Type type = assembly.GetType(nsName + ".Scanner");
                var s = (AScanner)Activator.CreateInstance(type);

                type = assembly.GetType(nsName + ".Parser");
                var p = (AParser)Activator.CreateInstance(type);

                p.InitParser(s);
                return p;
            }
            catch (Exception exception)
            {
                throw new XCompileException("Could not generate Parser and Scanner from grammar file!", exception);
            }
        }
        
    }
}
