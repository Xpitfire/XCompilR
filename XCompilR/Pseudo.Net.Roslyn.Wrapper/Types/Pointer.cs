using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Pointer<T> : ICloneable, IPseudoType
    where T : class {
    private T val;

    public Pointer() {
      val = null;
    }

    public Pointer(T value) {
      val = value;
    }

    public T Value {
      get { return val; }
      set { val = value; }
    }

    public object ValueRaw {
      get { return Value; }
      set { this.Value = (T)value; }
    }

    public static implicit operator T(Pointer<T> value) {
      return value.val;
    }

    public static bool operator ==(Pointer<T> left, Pointer<T> right) {
      Object o = right;
      if(o == null)
        return true;

      return (left.val == right.val);
    }

    public static bool operator !=(Pointer<T> left, Pointer<T> right) {
      Object o = right;
      if(o == null)
        return false;

      return (left.val != right.val);
    }

    public static implicit operator Pointer<T>(T value) {
      return new Pointer<T>(value);
    }

    public object Clone() {
      return new Pointer<T>(val);
    }

    public override string ToString() {
      return "->" + val.ToString();
    }
  }
}
