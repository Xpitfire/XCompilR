/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class AddressOfExpressionNode : ExpressionNode {
    public ExpressionNode AddressOf { get; private set; }
    public AddressOfExpressionNode(ExpressionNode adressof) {
      this.AddressOf = adressof;
    }

    public override TypeNode GetTypeNode() {
      return new PointerTypeNode(AddressOf.GetTypeNode());
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, AddressOf);
    }

    public override string ToString() {
      return String.Format("AddressOf({0})", AddressOf.ToString());
    }
  }
}
