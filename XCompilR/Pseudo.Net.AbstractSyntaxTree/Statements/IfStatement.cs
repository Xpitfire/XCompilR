/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class IfStatement : StatementNode {
    public ExpressionNode Expr { get; private set; }
    public StatementNode IfStat { get; set; }
    public StatementNode ElseStat { get; set; }

    public IfStatement(ExpressionNode expr, StatementNode ifstat) {
      this.Expr = expr;
      this.IfStat = ifstat;
    }

    public override bool ReturnValueOfType(TypeNode type) {
      bool ret = true;

      ret = IfStat.ReturnValueOfType(type);
      if(type.Basetype == TypeNode.BaseType.VOID) {
        if(ElseStat != null)
          ret = ret || ElseStat.ReturnValueOfType(type);
        else
          ret = true;
      } else {
        if(ElseStat != null)
          ret = ret && ElseStat.ReturnValueOfType(type);
        else
          ret = false;
      }
      return ret;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Expr);
      visitor(this, IfStat);
      if(ElseStat != null)
        visitor(this, ElseStat);
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("if");
      if(ElseStat != null)
        sb.Append(" else");
      return sb.ToString();
    }

    public override bool Validate(FaultHandler handleFault) {
      if(!Expr.GetTypeNode().IsBool())
        handleFault(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, Expr, 
          String.Format("cannot implicitly convert type '{0}' to 'bool'", 
            Expr.GetTypeNode().ToString()));
      return base.Validate(handleFault);
    }
  }
}
