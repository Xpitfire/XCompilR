/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class WhileStatement : StatementNode {
    public ExpressionNode Expr { get; private set; }
    public StatementNode Stat { get; private set; }

    public WhileStatement(ExpressionNode expr, StatementNode stat) {
      this.Expr = expr;
      this.Stat = stat;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Expr);
      visitor(this, Stat);
    }

    public override string ToString() {
      return "while";
    }

    public override bool Validate(FaultHandler handleFault) {
      if(!Expr.GetTypeNode().IsBool())
        handleFault(ErrorCode.IMPLICIT_CAST_NOT_SUPPORTED, 
          Expr,
          String.Format("cannot implicitly convert type '{0}' to 'BOOL'", 
            Expr.GetTypeNode().ToString()));

      return base.Validate(handleFault);
    }

    public override bool IsInLoopContext() {
      return true;
    }
  }


}
