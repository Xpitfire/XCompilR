/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class InvokeExpressionNode : ExpressionNode {
    public ArgumentList Arguments { get; private set; }
    public ExpressionNode Method { get; private set; }

    public InvokeExpressionNode(ExpressionNode func, ArgumentList pl) {
      this.Arguments = pl;
      this.Method = func;
    }

    public MethodDefinitionNode GetMethodDefinitionNode() {
      if(Method is MethodReferenceExpressionNode)
        return (Method as MethodReferenceExpressionNode).Method;

      if(Method is DereferenceMemberExpressionNode) {
        DereferenceMemberExpressionNode dr = Method as DereferenceMemberExpressionNode;
        ExpressionNode member = dr.GetMember();
        if(member is MethodReferenceExpressionNode)
          return (member as MethodReferenceExpressionNode).Method;
      } else if(Method is MemberSelectorExpressionNode) {
        MemberSelectorExpressionNode mse = Method as MemberSelectorExpressionNode;
        if(mse.MemberOf.GetTypeNode() is PointerTypeNode)
          throw new SemanticErrorException(ErrorCode.CANT_SELECT_MEMBER_OF_POINTER, mse.MemberOf, 
            "use '->' instead of '.' to select a member of a pointertype");

        var member = mse.MemberOf.GetTypeNode().GetMember(mse.Member);
        if(member is MethodReferenceExpressionNode)
          return (member as MethodReferenceExpressionNode).Method;
      }
      
      throw new SemanticErrorException(ErrorCode.FATAL_ERROR, this, 
        "Cannot resolve call expression to method definition");
    }

    public override bool Validate(FaultHandler handleFault) {
      if(GetMethodDefinitionNode().ParameterValidator != null) {
        if(!GetMethodDefinitionNode().ParameterValidator(Arguments, this))
          handleFault(ErrorCode.INVOKE_INVALID_PARAMETER, this, 
            "parameter validation failed!");
      } else {
        try {
          GetMethodDefinitionNode().ValidateParameter(Arguments, this);
        } catch(SemanticErrorException ex) {
          handleFault(ex.Code, null, ex.Message);
        }
      }

      return base.Validate(handleFault);
    }

    public override TypeNode GetTypeNode() {
      return GetMethodDefinitionNode().ReturnTypeResolver(Arguments);
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Method);
      foreach(ExpressionNode n in Arguments.Values) {
        visitor(this, n);
      }
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder(GetMethodDefinitionNode().FullName);
      sb.Append("( ");
      foreach(var p in Arguments) {
        sb.AppendFormat("{0} {1} ", p.Value.ToString(), p.Key.ToString());
      }
      sb.Append(")");
      return sb.ToString();
    }
  }
}
