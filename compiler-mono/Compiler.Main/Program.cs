using System;

namespace Compiler.Main
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string test1 = 
$@"var nTimes : int := 0;
// this is a line comment
// and this 
    print ""How many times ?"";
/* this
is
a //
block /*
comment */

    read nTimes;
    var x : int;
    for x in 0..nTimes - 1 do
        print x;
        print "" : Hello, World!\n"";
    end for;
    assert (x = nTimes);";

            string test2 = "a :=16664  + 2*( 3+6)/4  \n; var b : string := \"asdf\";// should be commented\n/* a := 1; \n asdf */";

            Compiler c = new Compiler(test1);
        }
    }
}
