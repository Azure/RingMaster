using System;
using System.Linq;

namespace NetStandard20Lib
{
    public class Class1
    {
        public void Print(string s)
        {
            Console.WriteLine($"input: {s}");
            Console.WriteLine(string.Join(",", Enumerable.Range(0, 5)));
        }
    }
}
