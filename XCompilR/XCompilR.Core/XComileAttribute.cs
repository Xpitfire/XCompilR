using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using SyntaxKind = Roslyn.Compilers.CSharp.SyntaxKind;

namespace XCompilR.Core
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [ContractClass(typeof(XCompileObject))]
    public class XCompileAttribute : Attribute
    {
        private ABindingLanguage Language { get; }
        private readonly string _targetCompilationName;

        public string Source { get; set; }

        public XCompileAttribute(string bindingLanguageAssembly, string sourceFile, string targetCompilationName)
        {
            Assembly assembly = Assembly.Load(bindingLanguageAssembly);
            Type type = assembly.GetType(bindingLanguageAssembly + ".BindingLanguage");
            Language = (ABindingLanguage)Activator.CreateInstance(type);
            _targetCompilationName = targetCompilationName;
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
            parser.BindingObjectMembers = bindingObj;
            parser.Parse(Source);
            
            // Creates the copimlation of a dll for the syntax tree received from the parser, 
            // adding references at runtime including metadata reference of System library
            var compilation = Compilation.Create(
                $"{_targetCompilationName}.dll", 
                new CompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                references: new[] {new MetadataFileReference(typeof (object).Assembly.Location)},
                syntaxTrees: new[] {parser.CompilationUnitSyntax.SyntaxTree}
                );

            // Here the compiled code is emitted into memory stream which is used to create a assembly at runtime 
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);
                assembly = Assembly.Load(stream.GetBuffer());
            }
            
            // Adding the new assembly to the dynamic object instance
            IDictionary<string, object> bindingObjProperties = bindingObj;
            bindingObjProperties.Add(Language.AssemblyName, assembly);
        }
    }
}
