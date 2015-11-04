using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Char : ICloneable, IPseudoType {
    private char val;

    public Char() {
      val = default(char);
    }

    public Char(char value) {
      val = value;
    }

    public char Value {
      get { return val; }
      set { val = value; }
    }

    public object ValueRaw {
      get { return Value; }
      set { this.Value = (char)value; }
    }

    public static implicit operator char(Char s) {
      return s.val;
    }

    public static implicit operator Char(char value) {
      return new Char(value);
    }

    public object Clone() {
      return new Char(this);
    }

    public override string ToString() {
      return val.ToString();
    }

  }
}
