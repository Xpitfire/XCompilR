using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XCompilR.Library;

namespace XCompilR.Core
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [ContractClass(typeof(XCompileObject))]
    public class XCompileAttribute : Attribute
    {
        public string TargetNamespace { get; set; }
        public string TargetMainClass { get; set; }

        private ABindingLanguage Language { get; }
        private readonly string _sourceFile;

        public XCompileAttribute(string bindingLanguageAssembly, string sourceFile)
        {
            // load import language assembly via reflection
            Assembly assembly = Assembly.Load(bindingLanguageAssembly);
            Type type = assembly.GetType(bindingLanguageAssembly + ".BindingLanguage");
            Language = (ABindingLanguage)Activator.CreateInstance(type);
            _sourceFile = sourceFile;
            // verify committed values
            if (TargetNamespace == null || TargetNamespace.Equals(string.Empty))
                TargetNamespace = bindingLanguageAssembly;
            if (_sourceFile == null || _sourceFile.Equals(string.Empty))
                throw new XCompileException("Invalid source file path!");
        }

        [XCompilRExceptionHandler(typeof(XCompileException))]
        public void BindMembers(dynamic bindingObj)
        {
            Type type = bindingObj.GetType();
            if (!type.IsSubclassOf(typeof(XCompileObject)))
            {
                throw new XCompileException("Invalid base class inheritance! Class does not derive from CrossCompileObject!");
            }

            var parser = Language.Parser;
            parser.BindingObject = bindingObj;
            parser.Parse(_sourceFile);

            if (parser.CompilationUnitSyntax == null)
            {
                if (TargetMainClass.Equals(string.Empty))
                    throw new XCompileException("Invalid TargetMainClass name!");
                parser.InitializeExecutableCompilationUnit(TargetNamespace, TargetMainClass);
            }

            // Creates a dll compilation from the syntax tree received from the parser and 
            // adds references at runtime including metadata references of System library
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create(
                $"{TargetMainClass}.dll",
                references: new[] { mscorlib },
                syntaxTrees: new[] { parser.CompilationUnitSyntax.SyntaxTree }
                );
            compilation.GetSemanticModel(parser.CompilationUnitSyntax.SyntaxTree, false);

            // The compiled code is emitted into memory stream which is used to create a assembly at runtime 
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);
                assembly = Assembly.Load(stream.GetBuffer());
            }

            // Adding the new assembly to the dynamic object instance
            bindingObj.Add(Language.AssemblyName, assembly);
            bindingObj.Add($"CreateInstanceOf{TargetMainClass}", new Func<object>(() => assembly.CreateInstance($"{TargetNamespace}.{TargetMainClass}")));
        }
        
    }
}
