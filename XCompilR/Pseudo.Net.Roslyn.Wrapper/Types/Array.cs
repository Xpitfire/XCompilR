using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PseudoToDotNetWrapper.Types {
  public class Array<T> : System.ICloneable where T : new() {
    private System.Array array;

    public Array(params int[] size) {
      array = ArrayUtils.InitializeArray<T>(size);
    }

    public Array(Array<T> a) {
      array = ArrayUtils.InitializeArray<T>(GetArraySize(a.array));
      Value = a.array;
    }

    private static int[] GetArraySize(System.Array a) {
      int[] size = new int[a.Rank];
      for(int i = 0; i < a.Rank; i++) {
        size[i] = a.GetUpperBound(i) + 1;
      }
      return size;
    }


    public static implicit operator System.Array(Array<T> s) {
      return s.array;
    }

    public System.Array Value {
      set {
        long[] indicies = new long[array.Rank];
        SetValue(0, value, indicies);
      }
      get { return this.array; }
    }

    private void SetValue(int dimension, System.Array src, long[] indicies) {
      for(int i = array.GetLowerBound(dimension); i <= array.GetUpperBound(dimension); i++) {
        indicies[dimension] = i;

        if(dimension < array.Rank - 1)
          SetValue(dimension + 1, src, indicies);
        else
          ((IPseudoType)(array.GetValue(indicies))).ValueRaw = ((IPseudoType)(src.GetValue(indicies))).ValueRaw;
        //((IPseudoType)(array.GetValue(indicies))).ValueRaw = ((IPseudoType)(src.GetValue(indicies))).Clone();
      }
    }

    public T this[params int[] index] {
      get { return (T)array.GetValue(index); }
      /*  set
        {
          //   array = (Array)array.Clone();
          ((IPseudoType)array.GetValue(index)).ValueRaw = ((IPseudoType)value).ValueRaw;
        }*/
    }

    public T this[int index] {
      get { return (T)array.GetValue(index); }
    }



    public object Clone() {
      return new Array<T>(this);
    }
  }
}
