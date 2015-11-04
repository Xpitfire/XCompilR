using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Integer : ICloneable, IPseudoType {
    private int val;

    public Integer() {
      val = default(int);
    }

    public Integer(int value) {
      val = value;
    }

    public int Value {
      get { return val; }
      set { val = value; }
    }

    public object ValueRaw {
      get { return Value; }
      set { this.Value = (int)value; }
    }

    public static implicit operator int(Integer s) {
      return s.val;
    }

    public static implicit operator Integer(int value) {
      return new Integer(value);
    }

    public static bool operator ==(Integer left, Integer right) {
      Object o = right;
      if(o == null)
        return false;

      return left.val == right.val;
    }

    public static bool operator !=(Integer left, Integer right) {
      Object o = right;
      if(o == null)
        return true;

      return left.val != right.val;
    }

    public override bool Equals(object obj) {
      if(obj is Integer) {
        return this.val == (obj as Integer).val;
      }
      return base.Equals(obj);
    }

    public override int GetHashCode() {
      return this.val.GetHashCode();
    }

    public static Integer operator +(Integer left, Integer right) {
      return new Integer(left.val + right.val);
    }

    public static Integer operator -(Integer left, Integer right) {
      return new Integer(left.val - right.val);
    }

    public static Integer operator *(Integer left, Integer right) {
      return new Integer(left.val * right.val);
    }

    public static Integer operator /(Integer left, Integer right) {
      return new Integer(left.val / right.val);
    }

    public static Integer operator %(Integer left, Integer right) {
      return new Integer(left.val % right.val);
    }

    public object Clone() {
      return new Integer(this);
    }

    public override string ToString() {
      return val.ToString();
    }

  }
}
