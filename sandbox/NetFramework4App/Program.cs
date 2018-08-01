using System;
using System.Linq;

namespace NetFramework4App
{
    class Program
    {
        static void Main(string[] args)
        {
            var c = new NetStandard20Lib.Class1();
            c.Print("Hello");
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(",", Enumerable.Range(2, 7)));
        }
    }
}
