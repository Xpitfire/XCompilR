/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class ProgramRootNode : Node {
    public TypeRepository types;
    public MethodDefinitionNode EntryPoint;

    public ProgramRootNode(AbstractSyntaxTree.Environment env) {
      this.Environment = env;
      this.types = new TypeRepository();
    }
    
    public string Name { 
      get { return Environment.Name; } 
      set { Environment.Name = value; } 
    }
    
    public AbstractSyntaxTree.Environment Environment { 
      get; 
      private set; 
    }

    public void Optimize() {
      // Try to move global variables, which are only used in the entrypoint method, to it.
      IList<string> removeList = new List<string>();

      foreach(VarReferenceExpressionNode v in Environment
        .OfType<VarReferenceExpressionNode>()) {
        if(v.GetReferenceFromList().Count() <= 1) {
          removeList.Add(v.Name);
        } else {
          var m = v.GetCompleteReferencedFromList().OfType<MethodDefinitionNode>();
          if(m.Count() == 1 && !v.IsConst && m.First() == EntryPoint) {
            v.IsStatic = false;
            m.First().AddVariable(v);
            removeList.Add(v.Name);
          }
        }
      }

      Environment.RemoveVariables(removeList);

      // remove unreferenced predefined methods (makes the graph smaller)
      var unrefMethods = Environment.OfType<PreDefinedMethodNode>()
                                    .Where(m => m.GetReferenceFromList().Count() <= 1)
                                    .ToArray();
                                    
      foreach(var m in unrefMethods) {
        m.Dispose();
      }

      ClearFlatGraphCache();
    }

    public override bool Validate(FaultHandler handleFault) {
      Node[] nodes = GetFlatGraph(true);

      foreach(Node n in nodes) {
        if(n is UndefinedTypeNode) {
          UndefinedTypeNode type = (n as UndefinedTypeNode);
          if(types.Exists(type.Typename)) {
            types.Replace(type.Typename, types.Find(type.Typename));
            //type.ResolvedTo = types.FindOrCreate(type.Typename);
          } else
            handleFault(ErrorCode.UNDEFINDED_TYPE, n, 
              FaultMessageBuilder.BuildUndefinedType(type.Typename));
          //throw new NotDefinedException(n, type.Typename);
        } else if(n is MethodReferenceExpressionNode) {
          MethodReferenceExpressionNode methodref = (n as MethodReferenceExpressionNode);
          if(!methodref.Method.IsDefined)
            throw new NotDefinedException(n, methodref.Name);
        }
      }

      nodes = GetFlatGraph(true);
      foreach(Node n in nodes) {
        n.Visit((p, c) => {
          c.AddReferencer(n);
        });
      }

      bool ok = true;
      foreach(Node n in nodes) {
        if(n != this) {
          if(!n.Validate(handleFault))
            ok = false;
        }
      }

      return ok;
    }

    private Node[] flatGraphCache;

    private void ClearFlatGraphCache() {
      flatGraphCache = null;
    }

    public Node[] GetFlatGraph(bool refresh = false) {
      if(flatGraphCache == null || refresh) {
        flatGraphCache = base.GetFlatGraph();
        foreach(var n in flatGraphCache) {
          n.Removed += (node) => {
            ClearFlatGraphCache();
          };
        }
      }
      return flatGraphCache;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, this.Environment);
      visitor(this, this.types);
      visitor(this, this.EntryPoint);
    }

    public override string ToString() {
      return String.Format("ProgramRootNode ({0})", Name);
    }
  }
}
