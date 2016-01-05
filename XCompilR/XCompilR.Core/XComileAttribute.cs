using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
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
        private ABindingLanguage Language { get; }
        private readonly string _targetNamcespace;
        private readonly string _targetMainClassName;

        public string Source { get; set; }

        public XCompileAttribute(string bindingLanguageAssembly, string sourceFile, string targetMainClassName, string targetNamespace = null)
        {
            Assembly assembly = Assembly.Load(bindingLanguageAssembly);
            Type type = assembly.GetType(bindingLanguageAssembly + ".BindingLanguage");
            Language = (ABindingLanguage)Activator.CreateInstance(type);
            _targetNamcespace = targetNamespace ?? bindingLanguageAssembly;
            _targetMainClassName = targetMainClassName;
            Source = sourceFile;
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
            parser.Parse(Source);
            
            if (parser.CompilationUnitSyntax == null)
                parser.InitializeExecutableCompilationUnit(_targetNamcespace, _targetMainClassName);

            // Creates the copimlation of a dll for the syntax tree received from the parser, 
            // adding references at runtime including metadata reference of System library
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create(
                $"{_targetMainClassName}.dll",
                references: new[] { mscorlib },
                syntaxTrees: new[] { parser.CompilationUnitSyntax.SyntaxTree }
                );
            compilation.GetSemanticModel(parser.CompilationUnitSyntax.SyntaxTree, false);

            // Here the compiled code is emitted into memory stream which is used to create a assembly at runtime 
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);
                assembly = Assembly.Load(stream.GetBuffer());
            }

            // Adding the new assembly to the dynamic object instance
            bindingObj.Add(Language.AssemblyName, assembly);
            bindingObj.Add($"CreateInstanceOf{_targetMainClassName}", new Func<object>(() => assembly.CreateInstance($"{_targetNamcespace}.{_targetMainClassName}")));
        }
        
    }
}
