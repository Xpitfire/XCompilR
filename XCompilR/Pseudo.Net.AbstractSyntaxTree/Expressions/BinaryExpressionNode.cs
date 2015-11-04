/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class BinaryExpressionNode : ExpressionNode {
    public enum Operator { UNDEF, AND, OR, EQ, NE, LT, LE, GT, GE, PLUS, MINUS, MULT, DIV, MOD, ISA };
    public Operator Op { get; private set; }
    public ExpressionNode LeftNode { get; private set; }
    public ExpressionNode RightNode { get; private set; }
    public BinaryExpressionNode(Operator op, ExpressionNode left, ExpressionNode right) {
      this.Op = op;
      this.LeftNode = left;
      this.RightNode = right;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, LeftNode);
      visitor(this, RightNode);
    }

    public override string ToString() {
      return String.Format("({0} {1} {2})", LeftNode.ToString(), Op.ToString(), RightNode.ToString());
    }

    public override TypeNode GetTypeNode() {
      switch(Op) {
        // only boolean
        case Operator.AND:
        case Operator.OR:
          if(TypeNode.AreBool(LeftNode.GetTypeNode(), RightNode.GetTypeNode()))
            return BaseTypeNode.BoolTypeNode;
          break;

        // numeric (perhaps string and char too)
        case Operator.LT:
        case Operator.LE:
        case Operator.GT:
        case Operator.GE:
          if(TypeNode.AreNumeric(LeftNode.GetTypeNode(), RightNode.GetTypeNode()))
            return BaseTypeNode.BoolTypeNode;

          if(LeftNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR &&
             RightNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR)
            return BaseTypeNode.BoolTypeNode;

          // Bool > >= <= < nicht erlauben, auch wenn im Alg&Dat Buch dies auf Seite 40 angefordert wird.
          //          if (TypeNode.AreBool(LeftNode.GetTypeNode(), RightNode.GetTypeNode()))
          //            return BaseTypeNode.BoolTypeNode;

          break;

        case Operator.PLUS:
          // numeric ( perhaps char too)
          if(LeftNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR &&
             RightNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR)
            return BaseTypeNode.CharTypeNode;

          // String concationation is explained in the AuD-Book. 
          // So I added it!
          if(LeftNode.GetTypeNode().Basetype == TypeNode.BaseType.STRING &&
              RightNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR)
            return BaseTypeNode.StringTypeNode;

          if(LeftNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR &&
              RightNode.GetTypeNode().Basetype == TypeNode.BaseType.STRING)
            return BaseTypeNode.StringTypeNode;

          goto case Operator.MINUS;

        case Operator.MINUS:
          if(LeftNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR &&
             RightNode.GetTypeNode().Basetype == TypeNode.BaseType.CHAR)
            return BaseTypeNode.CharTypeNode;

          goto case Operator.MULT;


        case Operator.MULT:
        case Operator.DIV:
          if(TypeNode.AreNumeric(LeftNode.GetTypeNode(), RightNode.GetTypeNode()))
            return BaseTypeNode.GetMax(LeftNode.GetTypeNode(), RightNode.GetTypeNode());

          break;

        // only int
        case Operator.MOD:
          if(TypeNode.AreNumeric(LeftNode.GetTypeNode(), RightNode.GetTypeNode()) && 
            RightNode.GetTypeNode().Basetype == TypeNode.BaseType.INT)
            return BaseTypeNode.IntegerTypeNode;
          break;

        // all
        case Operator.EQ:
        case Operator.NE:
          if(LeftNode.GetTypeNode().Basetype == RightNode.GetTypeNode().Basetype)
            return BaseTypeNode.BoolTypeNode;
          break;

        case Operator.ISA:
          if(LeftNode.GetTypeNode().IsClass() && RightNode.GetTypeNode().IsType()) {
            TypeReferenceExpressionNode t = RightNode as TypeReferenceExpressionNode;
            if(t.Type.IsClass())
              return BaseTypeNode.BoolTypeNode;
          }
          break;

      }

      throw new InvalidOperatorException(this, Op.ToString(), LeftNode.GetTypeNode().ToString(), RightNode.GetTypeNode().ToString());
    }

    public override bool Validate(FaultHandler handleFault) {
      try {
        GetTypeNode();
        if(Op == Operator.DIV && 
           RightNode.IsConst && 
           RightNode.GetConstValueExpression().GetTypeNode().IsNumeric()) {
          var r = RightNode.GetConstValueExpression();
          if((r is IntegerNode && (r as IntegerNode).Value==0) ||
             (r is RealNode && (r as RealNode).Value==0.0))
            handleFault(ErrorCode.DIVBYZERO, RightNode, "Attempted to divide by zero");
        }
      } catch(SemanticErrorException ex) {
        handleFault(ex.Code, this, ex.Message);
      }
      return base.Validate(handleFault);
    }

    public override bool IsConst {
      get {
        return LeftNode.IsConst && RightNode.IsConst;
      }
    }

    public override ConstExpressionNode GetConstValueExpression() {
      if(!IsConst)
        throw new InvalidOperationException("Binary Expression isn't const");

      ConstExpressionNode left = LeftNode.GetConstValueExpression();
      ConstExpressionNode right = RightNode.GetConstValueExpression();

      switch(Op) {
        case Operator.AND:
          return left.ReduceAnd(right);
        case Operator.OR:
          return left.ReduceOr(right);
        case Operator.EQ:
          return left.ReduceEquals(right);
        case Operator.NE:
          return left.ReduceEqualsNot(right);
        case Operator.LT:
          return left.ReduceLess(right);
        case Operator.LE:
          return left.ReduceLessOrEqual(right);
        case Operator.GT:
          return left.ReduceGreater(right);
        case Operator.GE:
          return left.ReduceGreaterOrEqual(right);
        case Operator.PLUS:
          return left.ReduceAdd(right);
        case Operator.MINUS:
          return left.ReduceSub(right);
        case Operator.MULT:
          return left.ReduceMult(right);
        case Operator.DIV:
          return left.ReduceDiv(right);
        case Operator.MOD:
          return left.ReduceMod(right);
      }

      throw new NotImplementedException();
    }

  }

}
