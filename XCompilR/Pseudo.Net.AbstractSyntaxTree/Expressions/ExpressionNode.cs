/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public abstract class ExpressionNode : Node {
    public virtual bool IsConst { get { return false; } }
    public virtual bool IsPublic { get { return true; } }

    public virtual ConstExpressionNode GetConstValueExpression() {
      if(!IsConst)
        throw new InvalidOperationException(
          "Expression must be const to create the const Expression");

      throw new NotImplementedException(String.Format(
        "There is no implementation for creating the const expression of this node ({0})", 
          GetType().Name));
    }

    public abstract TypeNode GetTypeNode();
  }

}
