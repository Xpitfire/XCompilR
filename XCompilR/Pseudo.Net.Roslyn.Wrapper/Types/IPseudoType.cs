using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public interface IPseudoType : System.ICloneable {
    Object ValueRaw { get; set; }
  }
}
