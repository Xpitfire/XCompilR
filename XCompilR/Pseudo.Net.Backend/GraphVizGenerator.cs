/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Pseudo.Net.AbstractSyntaxTree;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using Pseudo.Net.Backend;
using Pseudo.Net.Common;


namespace Pseudo.Net.Backend {
  public class GraphVizGenerator : BaseGenerator {

    private static Dictionary<Type, string> colors = new Dictionary<Type, string>() { 
      /*  {typeof(BaseTypeNode) , "yellow"},*/
        {typeof(ConstExpressionNode) , "#BB0066"},  
        {typeof(TypeNode) , "red"},
        {typeof(ExpressionNode) , "orange"},
        {typeof(StatementNode) , "green"},
        {typeof(Pseudo.Net.AbstractSyntaxTree.Environment) , "magenta"},
        {typeof(MethodDefinitionNode) , "blue"},
        
      };

    private static ISet<Type> hiddenNodeTypes = new HashSet<Type>()
    {
           typeof(BaseTypeNode),
           typeof(ConstExpressionNode),
      //     typeof(ExpressionNode)
      //typeof(PreDefinedMethodNode)
    };

    private static bool IsVisibleNode(Node n) {
      return !hiddenNodeTypes.Any((v) => { return n.GetType().Equals(v) || n.GetType().IsSubclassOf(v); });
    }

    public GraphVizGenerator(ProgramRootNode root, ReportErrorHandler errorHandler = null) : base(root, errorHandler) { }

    public override void Generate(Stream stream, Target target) {
      StreamWriter sw = new StreamWriter(stream);
      Node[] nodes = root.GetFlatGraph();
      sw.WriteLine("digraph g { graph [rankdir = \"LR\"];");
      sw.WriteLine("node [fontsize = \"10\"];");

      List<Node> nodeslist = new List<Node>(nodes);
      for(int i = 0; i < nodes.Length; i++) {
        Node n = nodes[i];
        if(IsVisibleNode(n)) {
          List<Node> childs = new List<Node>();
          n.Visit((p, c) => {
            // if (!(c is BaseTypeNode /*|| c is TypeRepository*/))
            childs.Add(c);
          });

          sw.Write("\"node{0}\" [label = \"{1}", i, n.GetType().Name);
          for(int j = 0; j < childs.Count; j++) {
            if(childs[j] is TypeNode)
              sw.Write("|<f{0}>{1}", j, Escape((childs[j] as TypeNode).GetName())); // add names
            else
              sw.Write("|<f{0}>{1}", j, Escape(childs[j].ToString())); // add names
          }

          sw.WriteLine("\" ");
          foreach(var c in colors) {
            if(n.GetType().Equals(c.Key) || n.GetType().IsSubclassOf(c.Key)) {
              sw.Write("color=\"{0}\" ", c.Value);
              break;
            }
          }

          sw.WriteLine("shape = \"record\" ];");

          for(int j = 0; j < childs.Count; j++) {
            if(IsVisibleNode(childs[j])) {
              if(!(n is AbstractSyntaxTree.Environment && childs[j] is AbstractSyntaxTree.Environment))
                sw.WriteLine("\"node{0}\":f{1} -> \"node{2}\";", i, j, nodeslist.IndexOf(childs[j]));
            }
          }


        }
      }

      #region Group Blocks

      Node.Visitor childVisitor = null;
      childVisitor = (p, c) => {
        if(c is BlockStatementNode) {
          GroupBlock(sw, c as BlockStatementNode, nodeslist);
        } else if(c is MethodDefinitionNode) {
          GroupMethod(sw, c as MethodDefinitionNode, nodeslist);
        } else if(c is StructTypeNode) {
          GroupClass(sw, c as StructTypeNode, nodeslist);
        } else if(IsVisibleNode(c) && !(c is TypeNode || c is VarReferenceExpressionNode)) {
          c.Visit(childVisitor);
        }
      };

      root.Visit(childVisitor);


      /*sw.Write("subgraph \"cluster_types\" {{label=\"types\"  ");
      foreach (Node n in nodes)
      {
        if (!(n is BaseTypeNode))
        {
          if (n is TypeNode || n is TypeRepository)
            sw.Write("node{0}; ", nodeslist.IndexOf(n));
        }
      }
      sw.WriteLine("}");
      */

      #endregion


      sw.WriteLine("}");
      sw.Close();

    }

    private static void GroupClass(StreamWriter sw, StructTypeNode method, IList<Node> nodelist) {
      sw.Write("subgraph \"cluster_nodeC{0}\" {{label=\"{1}\" node{0}; ", nodelist.IndexOf(method), method.FullName);
      Node.Visitor childVisitor = null;
      childVisitor = (p, c) => {
        if(c is MethodDefinitionNode) {
          GroupMethod(sw, c as MethodDefinitionNode, nodelist);
        } else
          if(c is StructTypeNode || (p is AbstractSyntaxTree.Environment && c is AbstractSyntaxTree.Environment)) {
            //      GroupClass(sw, c as ClassTypeNode, nodelist);
          } else
            if(IsVisibleNode(c) && !(c is TypeNode)) {
              if(c is VarReferenceExpressionNode) {
                if(p is AbstractSyntaxTree.Environment) {
                  sw.Write("node{0}; ", nodelist.IndexOf(c));
                  c.Visit(childVisitor);
                }
              } else {
                if(!(p is AbstractSyntaxTree.Environment && c is Pseudo.Net.AbstractSyntaxTree.Environment)) {
                  sw.Write("node{0}; ", nodelist.IndexOf(c));
                  c.Visit(childVisitor);
                }
              }
            }

      };

      method.Visit(childVisitor);

      sw.WriteLine("}");
    }

    private static void GroupMethod(StreamWriter sw, MethodDefinitionNode method, IList<Node> nodelist) {
      sw.Write("subgraph \"cluster_nodeM{0}\" {{label=\"{1}\" node{0}; ", nodelist.IndexOf(method), method.FullName);
      Node.Visitor childVisitor = null;
      childVisitor = (p, c) => {
        if(IsVisibleNode(c) && !(c is TypeNode || c is MethodDefinitionNode || c is StructTypeNode)) {
          if(c is VarReferenceExpressionNode) {
            if(p is AbstractSyntaxTree.Environment) {
              sw.Write("node{0}; ", nodelist.IndexOf(c));
              c.Visit(childVisitor);
            }
          } else {
            if(!(p is AbstractSyntaxTree.Environment && c is Pseudo.Net.AbstractSyntaxTree.Environment)) {
              sw.Write("node{0}; ", nodelist.IndexOf(c));
              c.Visit(childVisitor);
            }
          }
        }

      };

      //  if(!(method is PreDefinedMethodNode))
      method.Visit(childVisitor);

      sw.WriteLine("}");
    }

    private static void GroupBlock(StreamWriter sw, BlockStatementNode block, IList<Node> nodelist) {
      sw.Write("subgraph \"cluster_nodeB{0}\" {{ node{0}; ", nodelist.IndexOf(block));
      Node.Visitor childVisitor = null;
      childVisitor = (p, c) => {
        if(IsVisibleNode(c) && !(c is TypeNode || c is MethodDefinitionNode || c is StructTypeNode)) {
          if(c is VarReferenceExpressionNode) {
            if(p is AbstractSyntaxTree.Environment) {
              sw.Write("node{0}; ", nodelist.IndexOf(c));
              c.Visit(childVisitor);
            }
          } else {
            sw.Write("node{0}; ", nodelist.IndexOf(c));
            c.Visit(childVisitor);
          }
        }
      };

      block.Visit(childVisitor);

      sw.WriteLine("}");
    }

    #region Escaping

    private static Dictionary<string, string> escapeMapping = new Dictionary<string, string>()
    {
        {"\"", "\\\""},
        {"\\\\", "\\"},
        {"\a", @"\a"},
        {"\b", @"\b"},
        {"\f", @"\f"},
        {"\n", @"\n"},
        {"\r", @"\r"},
        {"\t", @"\t"},
        {"\v", @"\v"},
        {"\0", @"\0"},
        {">", "\\>"},
        {"<", "\\<"},
        {"}", "\\}"},
        {"{", "\\{"},
    };

    private static Regex escapeRegex = new Regex(string.Join("|", escapeMapping.Keys.ToArray()));

    private static string Escape(string s) {
      return escapeRegex.Replace(s, EscapeMatchEval);
    }

    private static string EscapeMatchEval(Match m) {
      if(escapeMapping.ContainsKey(m.Value)) {
        return escapeMapping[m.Value];
      }
      return escapeMapping[Regex.Escape(m.Value)];
    }

    #endregion

    public override Target[] SupportedTargets() {
      return new[] { Target.GV };
    }

  }
}
