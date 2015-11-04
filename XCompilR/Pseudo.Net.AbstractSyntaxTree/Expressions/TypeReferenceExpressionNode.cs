/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class TypeReferenceExpressionNode : ExpressionNode {
    public TypeNode Type { get; private set; }
    public TypeReferenceExpressionNode(TypeNode type) {
      this.Type = type;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.TypeTypeNode;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Type);
    }

    public override string ToString() {
      return Type.GetName();
    }
  }
}
