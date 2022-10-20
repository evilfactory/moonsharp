using System.Linq;
using MoonSharp.Interpreter.DataStructs;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.Units
{
	[TestFixture]
	public class FastStackTests
	{
		[Test]
		public void StackOverflow()
		{
			var stack = new FastStack<int>(0);
			Assert.Throws<StackOverflowException>(() => stack.Push(0));

			var strStack = new FastStack<string>(12);
			foreach (var _ in Enumerable.Range(0, 12))
				strStack.Push("");
			Assert.Throws<StackOverflowException>(() => strStack.Push(""));
		}

		[Test]
		public void CropWhenFull()
		{
			var stack = new FastStack<int>(5);
			foreach (var _ in Enumerable.Range(0, 5))
				stack.Push(0);
			stack.CropAtCount(3);
		}
	}
}
