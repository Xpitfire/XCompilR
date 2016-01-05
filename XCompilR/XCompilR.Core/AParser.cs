using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XCompilR.Core
{
    public abstract class AParser
    {
        public dynamic BindingObject { get; set; }
        public CompilationUnitSyntax CompilationUnitSyntax { get; set; }

        public void InitializeExecutableCompilationUnit(string @namespace, string mainClassName)
        {
            CompilationUnitSyntax = SyntaxFactory.CompilationUnit().AddMembers(
                SyntaxFactory.NamespaceDeclaration(
                    SyntaxFactory.IdentifierName(@namespace)).AddMembers(
                        SyntaxFactory.ClassDeclaration(mainClassName).AddMembers(
                            SyntaxFactory.MethodDeclaration(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Main")
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .WithBody(SyntaxFactory.Block()).AddBodyStatements(SyntaxFactory.ReturnStatement())
                            )
                    ));
        }

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void InitParser(AScanner scanner);

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void ReInitParser();

        //[XCompilRExceptionHandler(typeof(XCompileException), AttributeInheritance = MulticastInheritance.Multicast)]
        public abstract void Parse(string fileName);
    }
}
