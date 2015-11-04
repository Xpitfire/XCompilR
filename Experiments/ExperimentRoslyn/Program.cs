using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.CSharp;

namespace ExperimentRoslyn
{
    class Program
    {
        static void Main(string[] args)
        {
            var syntaxTree = SyntaxTree.ParseText(@"
using System;
class Demo
{
    static void Main()
    {
        if (true)
            Console.WriteLine(""Hello, World!"");
    }
}");
            var ifStatement = (IfStatementSyntax)syntaxTree
                .GetRoot()
                .DescendantNodes()
                .First(n => n.Kind == SyntaxKind.IfStatement);
            Console.WriteLine("Condition is '{0}'.", ifStatement);

            Console.ReadLine();
        }
    }
}
