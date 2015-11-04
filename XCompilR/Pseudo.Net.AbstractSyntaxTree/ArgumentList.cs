/* Pseudo.Net -- master thesis by thomas prückl 2013 */
/* University of Applied Sciences Upper Austria      */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pseudo.Net.AbstractSyntaxTree {
  public enum ArgumentDirection { IN, OUT, IO };

  public class ArgumentList 
  : IEnumerable<KeyValuePair<ExpressionNode, ArgumentDirection>> {
    private List<ExpressionNode> values;
    private List<ArgumentDirection> directions;

    public ArgumentList() {
      values = new List<ExpressionNode>();
      directions = new List<ArgumentDirection>();
    }

    public ArgumentList(ArgumentList original) {
      values = new List<ExpressionNode>(original.values);
      directions = new List<ArgumentDirection>(original.directions);
    }

    public void Add(ArgumentDirection direction, ExpressionNode value) {
      values.Add(value);
      directions.Add(direction);
    }

    public void Remove(int index) {
      if(index >= values.Count)
        throw new ArgumentOutOfRangeException();

      values.RemoveAt(index);
      directions.RemoveAt(index);
    }

    public IEnumerable<ExpressionNode> Values {
      get {
        return values;
      }
    }

    public IEnumerable<ArgumentDirection> Directions {
      get {
        return directions;
      }
    }

    public int Count {
      get {
        return values.Count;
      }
    }

    public IEnumerator<KeyValuePair<ExpressionNode, ArgumentDirection>> 
      GetEnumerator() {
      
      for(int i = 0; i < values.Count; i++) {
        yield return new KeyValuePair<
          ExpressionNode, 
          ArgumentDirection>(values[i], directions[i]);
      }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
  }
}
