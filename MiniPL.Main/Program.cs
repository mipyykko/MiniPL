using System;

namespace MiniPL.Main
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var test1 =
                @"var nTimes : int := 0;
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

            var test2 =
                "a :=16664  + 2*( 3+6)/4  \n; var b : string := \"asdf\";// should be commented\n/* a := 1; \n asdf */";

            var test3 = @"print ""Give a number: "";
var n : int;
read n;
var v : int := 1;
var i : int;
for i in 1..n do
    v := v * i;
end for;
print ""The result is: "";
print v;";
            var c = new Compiler(test3);
        }
    }
}