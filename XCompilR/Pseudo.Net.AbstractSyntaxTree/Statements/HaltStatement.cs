/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree {

  public class HaltStatement : StatementNode {
    public override void Visit(Node.Visitor visitor) {
    }

    public override string ToString() {
      return "halt";
    }

    public override bool ReturnValueOfType(TypeNode type) {
      return true;
    }
  }
}
