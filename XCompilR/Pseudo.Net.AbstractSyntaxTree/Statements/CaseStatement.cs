/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class CaseLabelStatement : StatementNode {
    public ExpressionNode Label { get; private set; }
    public StatementNode Statment { get; private set; }

    public CaseLabelStatement(ExpressionNode label, StatementNode stmt) {
      Label = label;
      Statment = stmt;
    }

    public override bool ReturnValueOfType(TypeNode type) {
      return Statment.ReturnValueOfType(type);
    }

    public override void Visit(Visitor visitor) {
      visitor(this, Label);
      visitor(this, Statment);
    }

    public override bool Validate(Node.FaultHandler handleFault) {
      if(!Label.IsConst)
        handleFault(ErrorCode.CASE_LABEL_NOT_CONST, Label, 
          "case label must be a const value!");

      return base.Validate(handleFault);
    }

    public override string ToString() {
      return String.Format("{0}:{1}", Label, Statment);
    }
  }

//----------------------------------------------------------------------------
//
//----------------------------------------------------------------------------
  public class CaseStatement : StatementNode {
    public ExpressionNode Expr { get; private set; }
    public StatementNode DefaultStatement { get; private set; }
    public List<CaseLabelStatement> Cases { get; private set; }

    public CaseStatement(ExpressionNode expr) {
      this.Expr = expr;
      this.Cases = new List<CaseLabelStatement>();
    }

    public void SetDefaultStatement(StatementNode n) {
      DefaultStatement = n;
    }

    public void AddCase(ExpressionNode e, StatementNode stmt) {
      Cases.Add(new CaseLabelStatement(e, stmt));
    }

    public override bool ReturnValueOfType(TypeNode type) {
      foreach(var c in Cases) {
        if(!c.ReturnValueOfType(type))
          return false;
      }

      if(DefaultStatement == null || !DefaultStatement.ReturnValueOfType(type))
        return false;

      return true;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Expr);
      if(DefaultStatement != null)
        visitor(this, DefaultStatement);

      foreach(var c in Cases) {
        visitor(this, c);
      }
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("case");
      return sb.ToString();
    }

    public override bool Validate(FaultHandler handleFault) {
      TypeNode t = Expr.GetTypeNode();
      foreach(var c in Cases) {
        if(!BaseTypeNode.AreAssignable(t, c.Label.GetTypeNode()))
          handleFault(ErrorCode.TYPES_NOT_COMPAREABLE, c.Label, 
            "case label type must be comparable to case expression!");
      }

      //if(DefaultStatement==null)
      //  handleFault(this, "no default label set!");

      List<ConstExpressionNode> labels = new List<ConstExpressionNode>();
      foreach(var c in Cases) {
        if(c.Label is ValueCollectionNode) {
          labels.AddRange((c.Label as ValueCollectionNode).GetValues());
        } else if(c.Label.IsConst) {
          labels.Add(c.Label.GetConstValueExpression());
        }
      }

      int allLabelsCount = labels.Count();
      int uniqueLabelCount = labels.Distinct().Count();
      if(allLabelsCount != uniqueLabelCount)
        handleFault(ErrorCode.CASE_LABEL_NOT_UNIQUE, this, 
          "label values must be unique");

      return base.Validate(handleFault);
    }
  }
}
