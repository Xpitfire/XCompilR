using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSourceLoad
{
    class Program
    {
        public static void Main()
        {
            dynamic d = new Demo();
            Console.WriteLine("result: " + d.Ident);
            Console.WriteLine("Successfully compiled dynamic object!");
        }
    }
}
