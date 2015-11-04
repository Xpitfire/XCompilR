using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class MutableString : System.ICloneable {
    private Char[] val;

   /* public MutableString Value {
      get { return this; }
      set {
        this.val = value.val.Select<Char, Char>(c => (Char)c.Clone()).ToArray<Char>();
      }
    }*/

    public string Value {
      get { return this; }
      set { 
        this.val = value.Select<char, Char>(c => new Char(c)).ToArray<Char>();
      }
    }


    public MutableString() {
      val = new Char[] { };
    }

    public MutableString(string s) {
      if(s != null) {
        this.val = s.Select<char, Char>(c => new Char(c)).ToArray<Char>();
      } else
        this.val = null;
    }

    public MutableString(MutableString s) {
      this.val = s.val.Select<Char, Char>(c => (Char)c.Clone()).ToArray<Char>();
    }

    public static implicit operator MutableString(string s) {
      return new MutableString(s);
    }

    public static implicit operator string(MutableString s) {
      return new string(s.val.Select<Char, char>(c => c.Value).ToArray<char>());
    }

    public static bool operator !=(MutableString a, MutableString b) {
      System.Object o = b;
      if(o == null)
        return true;

      return !(a == b);
    }

    public static bool operator ==(MutableString a, MutableString b) {
      System.Object o = b;
      if(o == null)
        return false;

      if(a.val.Length != b.val.Length)
        return false;

      for(int i = 0; i < a.val.Length; i++) {
        if(!a.val[i].Equals(b.val[i]))
          return false;
      }

      return true;
    }

    public override bool Equals(object obj) {
      if(obj is MutableString) {
        return val.Equals(((MutableString)obj).val);
      }
      return base.Equals(obj);
    }

    public override int GetHashCode() {
      return val.GetHashCode();
    }

    public int Length { get { return val.Length; } }

    public Char this[int index] {
      get { return val[index]; }
      /*     set {
             this.val[index] = value;
           }*/
    }

    public object Clone() {
      return new MutableString(this);
    }

    public override string ToString() {
      return this;
    }
  }
}
