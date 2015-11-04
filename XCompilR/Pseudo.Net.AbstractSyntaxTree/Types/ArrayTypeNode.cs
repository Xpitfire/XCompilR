/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pseudo.Net.AbstractSyntaxTree;
using Pseudo.Net.Exceptions;

namespace Pseudo.Net.AbstractSyntaxTree {
  public class ArrayTypeNode : TypeNode {
    public int Offset { get; private set; }
    public int Size { get; private set; }
    public TypeNode Typeof { get; set; }

    public ArrayTypeNode()
      : base(BaseType.ARRAY) { }

    public ArrayTypeNode(TypeNode type)
      : this() {
      Typeof = type;
    }

    public override void Visit(Node.Visitor visitor) {
      visitor(this, Typeof);
    }

    public void SetDimension(int minIdx, int maxIdx) {
      Offset = minIdx;
      Size = maxIdx - minIdx + 1;
    }

    public void SetDimension(int size) {
      this.Offset = 1;
      this.Size = size;
    }

    public bool IsInRange(int idx) {
      return idx >= Offset && idx < (Offset + Size);
    }

    public override TypeNode GetArrayType() {
      return Typeof;
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("array ");
      sb.AppendFormat("[{0}:{1}]", Offset, Offset + Size - 1);
      sb.Append(" of Type ");
      sb.Append(Typeof.GetName());
      return sb.ToString();
    }

  }
}
