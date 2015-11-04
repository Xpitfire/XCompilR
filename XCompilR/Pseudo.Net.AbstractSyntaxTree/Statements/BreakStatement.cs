/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class BreakStatement : StatementNode {
    public override void Visit(Node.Visitor visitor) {
    }

    public override string ToString() {
      return "break";
    }

    public override bool Validate(FaultHandler handleFault) {
      if(!GetCompleteReferencedFromList()
          .OfType<StatementNode>()
          .Any(s => s.IsInLoopContext()))
        handleFault(ErrorCode.BREAK_KEYWORD_NOT_ALLOWED, this, 
          "break only possible in a loop");

      return base.Validate(handleFault);
    }
  }

}
