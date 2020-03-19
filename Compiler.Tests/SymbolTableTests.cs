using System.Diagnostics.CodeAnalysis;
using Compiler.Common;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using NUnit.Framework;

namespace Compiler.Tests
{
    [ExcludeFromCodeCoverage]
    [TestFixture()]
    public class SymbolTableTests
    {
        private ISymbolTable _symbolTable => Context.SymbolTable;
        [SetUp]
        public void Setup()
        {
            Context.SymbolTable = new SymbolTable();
        }

        [Test]
        public void DeclareSymbol()
        {
            Assert.AreEqual(
                ErrorType.Unknown,
                _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int)
            );
        }

        [Test]
        public void DeclareSymbolExisting()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            Assert.AreEqual(
                ErrorType.RedeclaredVariable,
                _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int)
            );
        }

        [Test]
        public void LookupSymbol()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            Assert.AreEqual(
                PrimitiveType.Int,
                _symbolTable.LookupSymbol("kissa")
            );
        }
        
        [Test]
        public void LookupSymbolNotExists()
        {
            Assert.AreEqual(
                PrimitiveType.Void, // TODO: what
                _symbolTable.LookupSymbol("kissa")
            );
        }

        [Test]
        public void SymbolExists()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            Assert.True(_symbolTable.SymbolExists("kissa"));
            Assert.False(_symbolTable.SymbolExists("koira"));
        }

        [Test]
        public void SetControlVariable()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            Assert.AreEqual(
                ErrorType.Unknown,
                _symbolTable.SetControlVariable("kissa")
            );
            Assert.AreEqual(
                ErrorType.UndeclaredVariable,
                _symbolTable.SetControlVariable("koira")
            );
        }
        
        [Test]
        public void UnSetControlVariable()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            _symbolTable.SetControlVariable("kissa");

            Assert.AreEqual(
                ErrorType.Unknown,
                _symbolTable.UnsetControlVariable("kissa")
            );
            Assert.AreEqual(
                ErrorType.AssignmentToControlVariable, // TODO
                _symbolTable.UnsetControlVariable("koira")
            );
        }

        [Test]
        public void IsControlVariable()
        {
            _symbolTable.DeclareSymbol("kissa", PrimitiveType.Int);
            _symbolTable.DeclareSymbol("koira", PrimitiveType.String);
            _symbolTable.SetControlVariable("koira");
            Assert.True(_symbolTable.IsControlVariable("koira"));
            Assert.False(_symbolTable.IsControlVariable("kissa"));
            Assert.False(_symbolTable.IsControlVariable("hämähäkki"));
        }

    }
}