using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper {
  public static class BaseUtils {
    public static int Length(string s) {
      return s.Length;
    }

    public static int Low(int val) {
      return int.MinValue;
    }

    public static int High(int val) {
      return int.MaxValue;
    }

    public static float Low(float val) {
      return float.MinValue;
    }

    public static float High(float val) {
      return float.MaxValue;
    }
  }
}
