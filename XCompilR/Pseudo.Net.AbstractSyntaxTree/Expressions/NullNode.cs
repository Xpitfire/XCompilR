/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class NullNode : ConstExpressionNode {
    public NullNode() {
    }

    public override void Visit(Node.Visitor visitor) {
    }

    public override TypeNode GetTypeNode() {
      return new PointerTypeNode(BaseTypeNode.VoidTypeNode);
    }

    public override string ToString() {
      return "null";
    }

    public override string GetValueLiteral() {
      return "null";
    }

    public override object GetValue() {
      return "null";
    }
  }

}
