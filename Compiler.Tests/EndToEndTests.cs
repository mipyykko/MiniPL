using System;
using System.IO;
using Compiler.Common;
using Compiler.Common.Errors;
using Compiler.Parse;
using Compiler.Scan;
using NUnit.Framework;
using Snapper;
using Snapper.Attributes;

namespace Compiler.Tests
{
    public class EndToEndTests
    {
        [TearDown]
        public void Teardown()
        {
            var sin = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(sin); 
            
            var sout = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(sout);
        }

    }
}