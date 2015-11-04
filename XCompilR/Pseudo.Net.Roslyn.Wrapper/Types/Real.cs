using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Real : ICloneable, IPseudoType {
    private double val;

    public Real() {
      val = default(double);
    }

    public Real(double value) {
      val = value;
    }

    public double Value {
      get { return val; }
      set { val = value; }
    }

    public object ValueRaw {
      get { return Value; }
      set { this.Value = (double)value; }
    }

    public static implicit operator double(Real s) {
      return s.val;
    }

    public static implicit operator Real(double value) {
      return new Real(value);
    }

    public static Real operator +(Real left, Real right) {
      return new Real(left.val + right.val);
    }

    public static Real operator -(Real left, Real right) {
      return new Real(left.val - right.val);
    }

    public static Real operator *(Real left, Real right) {
      return new Real(left.val * right.val);
    }

    public static Real operator /(Real left, Real right) {
      return new Real(left.val / right.val);
    }

    public object Clone() {
      return new Real(this);
    }

    public override string ToString() {
      return val.ToString();
    }
  }
}
