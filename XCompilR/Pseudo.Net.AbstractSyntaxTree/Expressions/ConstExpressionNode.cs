/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.Exceptions;
using System.Globalization;

namespace Pseudo.Net.AbstractSyntaxTree {
  public abstract class ConstExpressionNode : ExpressionNode {
    public override void Visit(Node.Visitor visitor) {
    }

    public abstract string GetValueLiteral();
    public abstract object GetValue();

    public override ConstExpressionNode GetConstValueExpression() {
      return this;
    }

    public override bool IsConst {
      get {
        return true;
      }
    }

    public virtual BoolNode ReduceAnd(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual BoolNode ReduceOr(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual BoolNode ReduceEquals(ConstExpressionNode val) {
      return new BoolNode(this.Equals(val));
    }

    public virtual BoolNode ReduceEqualsNot(ConstExpressionNode val) {
      return new BoolNode(!this.Equals(val));
    }

    public virtual BoolNode ReduceLessOrEqual(ConstExpressionNode val) {
      BoolNode eq = ReduceEquals(val);
      if(eq.Value)
        return eq;

      return ReduceGreater(val).ReduceNot();
    }

    public virtual BoolNode ReduceLess(ConstExpressionNode val) {
      return ReduceGreaterOrEqual(val).ReduceNot();
    }

    public virtual BoolNode ReduceGreater(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual BoolNode ReduceGreaterOrEqual(ConstExpressionNode val) {
      BoolNode ge = ReduceGreater(val);
      if(ge.Value)
        return ge;

      return ReduceEquals(val);
    }

    public virtual ConstExpressionNode ReduceAdd(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReduceSub(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReduceMult(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReduceDiv(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReduceMod(ConstExpressionNode val) {
      throw new NotSupportedException();
    }

    public virtual BoolNode ReduceNot() {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReducePlus() {
      throw new NotSupportedException();
    }

    public virtual ConstExpressionNode ReduceMinus() {
      throw new NotSupportedException();
    }

    public override bool Equals(object obj) {
      if(obj is ConstExpressionNode) {
        return GetValue().Equals((obj as ConstExpressionNode).GetValue());
      }
      return base.Equals(obj);
    }

    public override int GetHashCode() {
      return GetValue().GetHashCode();
    }

    public virtual ConstExpressionNode Increment() {
      if(!GetTypeNode().HasIncrement())
        throw new InvalidOperationException(String.Format(
          "Increment operation is not possible on the type '{0}'", GetTypeNode()));

      throw new NotImplementedException(String.Format(
        "No Increment method for '{0}' implemented", this.GetType().Name));
    }

  };

//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------  
  public class IntegerNode : ConstExpressionNode {
    public int Value { get; private set; }
    public IntegerNode(int val) {
      this.Value = val;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.IntegerTypeNode;
    }

    public override string ToString() {
      return String.Format("int({0})", Value);
    }

    public override string GetValueLiteral() {
      return Value.ToString();
    }

    public override object GetValue() {
      return Value;
    }

    public override BoolNode ReduceGreater(ConstExpressionNode val) {
      if(val is IntegerNode) {
        IntegerNode c = val as IntegerNode;
        return new BoolNode(this.Value > c.Value);
      }
      return base.ReduceGreater(val);
    }

    public override ConstExpressionNode ReduceAdd(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new IntegerNode(this.Value + (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value + (val as RealNode).Value);
      }
      return base.ReduceAdd(val);
    }

    public override ConstExpressionNode ReduceDiv(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new IntegerNode(this.Value / (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value / (val as RealNode).Value);
      }
      return base.ReduceDiv(val);
    }

    public override ConstExpressionNode ReduceMod(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new IntegerNode(this.Value % (val as IntegerNode).Value);
      }

      return base.ReduceMod(val);
    }

    public override ConstExpressionNode ReduceMult(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new IntegerNode(this.Value * (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value * (val as RealNode).Value);
      }
      return base.ReduceMult(val);
    }

    public override ConstExpressionNode ReduceSub(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new IntegerNode(this.Value - (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value - (val as RealNode).Value);
      }
      return base.ReduceSub(val);
    }

    public override ConstExpressionNode ReducePlus() {
      return this;
    }

    public override ConstExpressionNode ReduceMinus() {
      return new IntegerNode(-Value);
    }

    public override ConstExpressionNode Increment() {
      return new IntegerNode(Value + 1);
    }
  }

//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------  
  public class RealNode : ConstExpressionNode {
    public double Value { get; private set; }
    public RealNode(double val) {
      this.Value = val;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.RealTypeNode;
    }

    public override string ToString() {
      return String.Format("real({0})", Value);
    }

    public override string GetValueLiteral() {
      return Value.ToString(CultureInfo.InvariantCulture) + "F";
    }

    public override object GetValue() {
      return Value;
    }

    public override BoolNode ReduceGreater(ConstExpressionNode val) {
      if(val is RealNode) {
        RealNode c = val as RealNode;
        return new BoolNode(this.Value > c.Value);
      }
      return base.ReduceGreater(val);
    }

    public override ConstExpressionNode ReduceAdd(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new RealNode(this.Value + (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value + (val as RealNode).Value);
      }
      return base.ReduceAdd(val);
    }

    public override ConstExpressionNode ReduceDiv(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new RealNode(this.Value / (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value / (val as RealNode).Value);
      }
      return base.ReduceDiv(val);
    }

    public override ConstExpressionNode ReduceMult(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new RealNode(this.Value * (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value * (val as RealNode).Value);
      }
      return base.ReduceMult(val);
    }

    public override ConstExpressionNode ReduceSub(ConstExpressionNode val) {
      if(val is IntegerNode) {
        return new RealNode(this.Value - (val as IntegerNode).Value);
      } else if(val is RealNode) {
        return new RealNode(this.Value - (val as RealNode).Value);
      }
      return base.ReduceSub(val);
    }

    public override ConstExpressionNode ReducePlus() {
      return this;
    }

    public override ConstExpressionNode ReduceMinus() {
      return new RealNode(-Value);
    }
  }

//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------    
  public class StringNode : ConstExpressionNode {
    public string Value { get; private set; }
    public StringNode(string val) {
      this.Value = val;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.StringTypeNode;
    }

    public override string ToString() {
      return String.Format("string({0})", Value);
    }

    public override string GetValueLiteral() {
      return String.Format("\"{0}\"", Value);
    }

    public override object GetValue() {
      return Value;
    }

    public override BoolNode ReduceGreater(ConstExpressionNode val) {
      if(val is StringNode) {
        StringNode c = val as StringNode;
        return new BoolNode(this.Value.CompareTo(c.Value) > 0);
      }
      return base.ReduceGreater(val);
    }
  }

//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------  
  public class CharNode : ConstExpressionNode {
    public char Value { get; private set; }
    public CharNode(char val) {
      this.Value = val;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.CharTypeNode;
    }

    public override string ToString() {
      return String.Format("char({0})", Value);
    }

    public override string GetValueLiteral() {
      return String.Format("'{0}'", Value);
    }

    public override object GetValue() {
      return Value;
    }

    public override BoolNode ReduceGreater(ConstExpressionNode val) {
      if(val is CharNode) {
        CharNode c = val as CharNode;
        return new BoolNode(this.Value > c.Value);
      }
      return base.ReduceGreater(val);
    }

    public override ConstExpressionNode Increment() {
      return new CharNode(Convert.ToChar(Convert.ToInt32(Value) + 1));
    }

  }

//-------------------------------------------------------------------------
//
//-------------------------------------------------------------------------  
  public class BoolNode : ConstExpressionNode {
    public bool Value { get; private set; }
    public BoolNode(bool val) {
      this.Value = val;
    }

    public override TypeNode GetTypeNode() {
      return BaseTypeNode.BoolTypeNode;
    }

    public override string ToString() {
      return String.Format("bool({0})", Value);
    }

    public override string GetValueLiteral() {
      if(Value)
        return "true";
      else
        return "false";
    }

    public override object GetValue() {
      return Value;
    }

    public override BoolNode ReduceAnd(ConstExpressionNode val) {
      if(val is BoolNode) {
        BoolNode v = val as BoolNode;
        return new BoolNode(v.Value && this.Value);
      }
      return base.ReduceAnd(val);
    }

    public override BoolNode ReduceOr(ConstExpressionNode val) {
      if(val is BoolNode) {
        BoolNode v = val as BoolNode;
        return new BoolNode(v.Value || this.Value);
      }
      return base.ReduceOr(val);
    }

    public override BoolNode ReduceNot() {
      return new BoolNode(!this.Value);
    }
  }

}
