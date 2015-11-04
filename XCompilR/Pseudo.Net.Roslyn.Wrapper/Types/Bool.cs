using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Bool : ICloneable, IPseudoType {
    private bool val;

    public Bool() {
      val = default(bool);
    }

    public Bool(bool value) {
      val = value;
    }

    public bool Value {
      get { return val; }
      set { val = value; }
    }

    public static implicit operator bool(Bool s) {
      return s.val;
    }

    public static implicit operator Bool(bool value) {
      return new Bool(value);
    }

    public object Clone() {
      return new Bool(this);
    }


    public object ValueRaw {
      get { return Value; }
      set { this.Value = (bool)value; }
    }

    public override string ToString() {
      return val.ToString();
    }
  }
}
