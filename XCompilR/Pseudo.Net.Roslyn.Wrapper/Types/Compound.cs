using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PseudoToDotNetWrapper;

namespace PseudoToDotNetWrapper.Types {
  public abstract class Compound : ICloneable, IPseudoType {
    public Object Clone() {
      Type t = this.GetType();
      Object ret = Activator.CreateInstance(t);
      foreach(var v in t.GetFields()) {
        if(v.FieldType.GetInterface(typeof(ICloneable).FullName) != null) {
          ICloneable val = v.GetValue(this) as ICloneable;
          v.SetValue(ret, val.Clone());
        } else
          v.SetValue(ret, v.GetValue(this));
      }
      return ret;
    }

    private void Set(Compound c) {
      Type t = this.GetType();

      foreach(var v in t.GetFields()) {
        if(v.FieldType.GetInterface(typeof(ICloneable).FullName) != null) {
          ICloneable val = v.GetValue(c) as ICloneable;
          v.SetValue(this, val.Clone());
        } else
          v.SetValue(this, v.GetValue(this));
      }
    }

    public Compound Value {
      get { return this; }
      set { Set(value); }
    }

    public object ValueRaw {
      get { return this; }
      set { this.Value = (Compound)value; }
    }

    public J Clone<J>() {
      return (J)Clone();
    }
  }
}
