using System;
using System.Collections.Generic;
using System.IO;
using MiniPL.Common;
using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;
using MiniPL.Interpret;
using MiniPL.Parse;
using MiniPL.Scan;
using NUnit.Framework;
using Snapper;

namespace MiniPL.Tests
{
    [TestFixture]
    public class TreeTests
    {
        private static Scanner scanner;
        private static Parser parser;

        [SetUp]
        public void Setup()
        {
            Context.ErrorService = new ErrorService();
            Context.SymbolTable = new SymbolTable();

            scanner = new Scanner();
            parser = new Parser(scanner);
        }

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

        [TestFixture]
        public class Snapshots
        {
            private static IEnumerable<TestCaseData> Programs()
            {
                yield return new TestCaseData("Program1", TestUtil.Program1);
                yield return new TestCaseData("Program2", TestUtil.Program2);
                yield return new TestCaseData("Program3", TestUtil.Program3);
            }

            [SetUp]
            public void Setup()
            {
                Context.ErrorService = new ErrorService();
                Context.SymbolTable = new SymbolTable();

                scanner = new Scanner();
                parser = new Parser(scanner);
            }

            [Test]
            [TestCaseSource(nameof(Programs))]
            public void ParserTreeTest(string test, string source)
            {
                Context.Source = Text.Of(source);
                var tree = parser.Program();
                tree.ShouldMatchChildSnapshot(test);
            }

            [Test]
            [TestCaseSource(nameof(Programs))]
            public void ParserASTTest(string test, string source)
            {
                var output = new StringWriter();
                Console.SetOut(output);

                Context.Source = Text.Of(source);
                var tree = parser.Program();
                tree.AST();
                output.ToString().ShouldMatchChildSnapshot(test);
            }

            [Test]
            [TestCaseSource(nameof(Programs))]
            public void SemanticAnalyzerTreeTest(string test, string source)
            {
                Context.Source = Text.Of(source);
                var tree = parser.Program();
                var symbolTableVisitor = new SemanticAnalysisVisitor();
                tree.Accept(symbolTableVisitor);
                tree.ShouldMatchChildSnapshot(test);
            }
            
            [Test]
            [TestCaseSource(nameof(Programs))]
            public void SemanticAnalyzerASTTest(string test, string source)
            {
                var output = new StringWriter();
                Console.SetOut(output);

                Context.Source = Text.Of(source);
                var tree = parser.Program();
                var symbolTableVisitor = new SemanticAnalysisVisitor();
                tree.Accept(symbolTableVisitor);
                tree.AST();
                output.ToString().ShouldMatchChildSnapshot(test);
            }
        }
    }
}