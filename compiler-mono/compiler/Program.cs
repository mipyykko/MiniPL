using System;

namespace Compiler
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string test1 = 
$@"var nTimes : int := 0;
    print ""How many times ?"";
    read nTimes;
    var x : int;
    for x in 0..nTimes - 1 do
        print x;
        print "" : Hello, World!\n"";
    end for;
    assert (x = nTimes);";

            string test2 = "a :=16664  + 2*( 3+6)/4  \n; var b : string := \"asdf\";// should be commented\n/* a := 1; \n asdf */";

            Scanner s = new Scanner(new Text(test1));
            Console.WriteLine(s.Scan());
        }
    }
}
