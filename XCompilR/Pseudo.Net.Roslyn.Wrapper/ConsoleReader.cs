using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PseudoToDotNetWrapper.Types;

namespace PseudoToDotNetWrapper {
  public static class ConsoleReader {
    public static void Read(Types.Integer val) {
      Console.Write("(int) > ");
      string input = Console.ReadLine();
      int v;
      int.TryParse(input, out v);
      val.Value = v;
    }

    public static void Read(Types.Real val) {
      Console.Write("(double) > ");
      string input = Console.ReadLine();
      double v;
      double.TryParse(input, out v);
      val.Value = v;
    }

    public static void Read(Types.MutableString val) {
      Console.Write("(string) > ");
      val.Value = Console.ReadLine();
    }

#if false
    public static void Read(out string val) {
      Console.Write("(string) > ");
      val = Console.ReadLine();
    }

    public static void Read(out int val) {
      Console.Write("(int) > ");
      string input = Console.ReadLine();
      int v;
      int.TryParse(input, out v);
      val = v;
    }

    public static void Read(out double val) {
      Console.Write("(double) > ");
      string input = Console.ReadLine();
      double v;
      double.TryParse(input, out v);
      val = v;
    }
#endif

    public static void Read() {
      Console.ReadLine();
    }
  }
}
