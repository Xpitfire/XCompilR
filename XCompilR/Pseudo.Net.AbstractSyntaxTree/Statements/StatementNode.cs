/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree
{
  public abstract class StatementNode : Node
  {
    public virtual bool ReturnValueOfType(TypeNode type) { 
      return type.Basetype == TypeNode.BaseType.VOID; 
    }
    
    public virtual bool IsContructorCall() { return false; }
    public virtual bool IsInLoopContext() { return false; }

    public Node[] GetStatementExpressionNodes() {

      ISet<Node> nodes = new HashSet<Node>();
      Visitor collector = null;

      collector = (p, c) => {
        if(c is StatementNode || c is ExpressionNode) {
          if(!nodes.Contains(c)) {
            nodes.Add(c);
            c.Visit(collector);
          }
        }
      };

      nodes.Add(this);
      this.Visit(collector);
      return nodes.ToArray();
    }
  }
}
