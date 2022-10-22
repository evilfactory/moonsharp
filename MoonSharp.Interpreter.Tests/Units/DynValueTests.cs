using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.Units {
	public class DynValueTests {
		[Test]
		public void Equality()
		{
			Assert.AreEqual(DynValue.True, DynValue.True);
			Assert.AreNotEqual(DynValue.False, DynValue.True);
			Assert.AreEqual(DynValue.NewBoolean(true), DynValue.True);
			Assert.AreNotEqual(DynValue.NewBoolean(false), DynValue.True);

			Assert.AreEqual(DynValue.Nil, DynValue.NewNil());
			Assert.AreNotEqual(DynValue.Nil, null);

			Assert.AreEqual(DynValue.NewTuple(DynValue.Nil, DynValue.True, DynValue.NewNumber(42)),
				DynValue.NewTuple(DynValue.Nil, DynValue.True, DynValue.NewNumber(42)));
		}
	}
}
