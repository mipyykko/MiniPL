using MiniPL.Common;
using MiniPL.Common.Symbols;
using MiniPL.Interpret;
using Moq;
using NUnit.Framework;

namespace MiniPL.Tests
{
    public class ProgramMemoryTests
    {
        private Mock<SymbolTable> symbolTableMock;
        private ISymbolTable symbolTable;
        private ProgramMemory memory;
        
        [SetUp]
        public void Setup()
        {
            symbolTableMock = new Mock<SymbolTable>
            {
                CallBase = true
            };

            symbolTable = symbolTableMock.Object;
            symbolTable.DeclareSymbol("a", PrimitiveType.Int);
            symbolTable.DeclareSymbol("b", PrimitiveType.Int);
            symbolTable.DeclareSymbol("c", PrimitiveType.String);
            symbolTable.DeclareSymbol("d", PrimitiveType.Bool);
            symbolTable.SetControlVariable("a");

            Context.SymbolTable = symbolTable;
            memory = new ProgramMemory();
            memory.UpdateVariable("b", "666");
        }
    
        [TestCase("a", "6", 6, ErrorType.AssignmentToControlVariable)]
        [TestCase("b", "1", 1)]
        [TestCase("b", "kissa", "kissa", ErrorType.TypeError)]
        [TestCase("c", "6", "6")]
        [TestCase("d", "true", true)]
        [TestCase("d", "false", false)]
        [TestCase("d", "kissa", "kissa", ErrorType.TypeError)]
        public void UpdateVariableTests(string id, object value, object expected, ErrorType et = ErrorType.Unknown)
        {
            var ret = memory.UpdateVariable(id, value);
            Assert.AreEqual(et, ret);
            if (et == ErrorType.Unknown)
            {
                Assert.AreEqual(memory.LookupVariable(id), expected);
            }
        }

        [TestCase("a", 6)]
        public void UpdateControlVariableTests(string id, object value, ErrorType et = ErrorType.Unknown)
        {
            var ret = memory.UpdateControlVariable(id, value);
            Assert.AreEqual(et, ret);
            Assert.AreEqual(memory.LookupVariable("a"), (int) value);
        }

        [Test]
        public void LookUpVariableTest()
        {
            Assert.AreEqual(666, memory.LookupVariable("b"));
        }
    }
}