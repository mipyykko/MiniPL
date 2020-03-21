using MiniPL.Common;

namespace MiniPL.Tests
{
    public static class TestUtil
    {
        public static SourceInfo MockSourceInfo = SourceInfo.Of((0, 0), (0, 0, 0));
        public static string Program1 = $@"var X : int := 4 + (6 * 2);
print X;";

        public static string Program2 = $@"var nTimes : int := 0;
 print ""How many times?"";
read nTimes;
var x : int;
for x in 0..nTimes-1 do
print x;
print "" : Hello, World!\n"";
end for;
assert (x = nTimes);";
        public static string Program3 = $@"print ""Give a number: "";
var n : int;
read n;
var v : int := 1;
var i : int;
for i in 1..n do
    v := v * i;
end for;
print ""The result is: "";
print v;";

    }
}