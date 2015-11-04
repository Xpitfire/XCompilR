/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class BlockStatementNode : StatementNode {
    private IList<StatementNode> statements;

    public BlockStatementNode() {
      statements = new List<StatementNode>();
    }

    public override void Visit(Node.Visitor visitor) {
      foreach(var s in statements) {
        visitor(this, s);
      }
    }

    public void AddStatement(StatementNode statement) {
      statements.Add(statement);
    }

    public override bool ReturnValueOfType(TypeNode type) {
      if(statements.Count > 0)
        return statements.Last().ReturnValueOfType(type);

      if(type.Basetype == TypeNode.BaseType.VOID)
        return true;

      return false;
    }

    public override string ToString() {
      return "block";
    }

    public StatementNode[] GetStatementes() {
      return statements.ToArray();
    }
  }

}
