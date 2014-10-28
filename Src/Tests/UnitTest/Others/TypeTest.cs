using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.Compilers.Types;

namespace UnitTest.Others
{
    [TestClass]
    public class TypeTest
    {
        [TestMethod]
        public void 型の大小比較()
        {
            var testcases = new[]
            {
                //new { X = BuiltInNumericType.Int32 > BuiltInNumericType.Int32, Z = false },                
                //new { X = BuiltInNumericType.Int32 >= BuiltInNumericType.Int32, Z = true },
                //new { X = BuiltInNumericType.Int32 <= BuiltInNumericType.Int32, Z = true },
                //new { X = BuiltInNumericType.Int32 < BuiltInNumericType.Int32, Z = false },
                //new { X = BuiltInNumericType.Int32 == BuiltInNumericType.Int32, Z = true },
                //new { X = BuiltInNumericType.Int32 != BuiltInNumericType.Int32, Z = false },
                new { X = BuiltInNumericType.Int64 > BuiltInNumericType.Int32, Z = true },
                new { X = BuiltInNumericType.Int64 >= BuiltInNumericType.Int32, Z = true },
                new { X = BuiltInNumericType.Int64 <= BuiltInNumericType.Int32, Z = false },
                new { X = BuiltInNumericType.Int64 < BuiltInNumericType.Int32, Z = false },
                new { X = BuiltInNumericType.Int64 == BuiltInNumericType.Int32, Z = false },
                new { X = BuiltInNumericType.Int64 != BuiltInNumericType.Int32, Z = true },
                new { X = BuiltInNumericType.Float64 > BuiltInNumericType.Int64, Z = true },
                new { X = BuiltInNumericType.Float64 >= BuiltInNumericType.Int64, Z = true },
                new { X = BuiltInNumericType.Float64 <= BuiltInNumericType.Int64, Z = false },
                new { X = BuiltInNumericType.Float64 < BuiltInNumericType.Int64, Z = false },
                new { X = BuiltInNumericType.Float64 == BuiltInNumericType.Int64, Z = false },
                new { X = BuiltInNumericType.Float64 != BuiltInNumericType.Int64, Z = true },
            };


            for (int i = 0; i < testcases.Length; i++)
            {
                Assert.AreEqual(testcases[i].Z, testcases[i].X);
            }
        }
    }
}
