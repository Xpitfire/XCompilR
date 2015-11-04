using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper {
  internal static class ArrayUtils {
    public static Array InitializeArray<T>(params int[] size) where T : new() {
      Array array = Array.CreateInstance(typeof(T), size);
      //array.Initialize();
      array.Populate<T>();
      //   ValueArray<int> a = new ValueArray<int>(1, 2, 3);
      //   ValueArray<int> b = a;
      return array;
    }

    private static void Populate<T>(this Array array) where T : new() {
      int[] indicies = new int[array.Rank];

      PopulateDimension<T>(array, indicies, 0);
    }

    private static void PopulateDimension<T>(Array array, int[] indicies, int dimension) where T : new() {
      for(int i = 0; i <= array.GetUpperBound(dimension); i++) {
        indicies[dimension] = i;

        if(dimension < array.Rank - 1)
          PopulateDimension<T>(array, indicies, dimension + 1);
        else
          array.SetValue(new T(), indicies);
      }
    }
  }
}
