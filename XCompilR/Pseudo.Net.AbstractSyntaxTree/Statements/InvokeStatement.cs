/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class InvokeStatementNode : StatementNode {
    public InvokeExpressionNode Function { get; private set; }
    public bool IsConstructor { private get; set; }

    public InvokeStatementNode(InvokeExpressionNode function) {
      this.Function = function;
      this.IsConstructor = false;
    }

    public override bool IsContructorCall() {
      return IsConstructor;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Function);
    }

    public override string ToString() {
      return "call";
    }
  }
}
