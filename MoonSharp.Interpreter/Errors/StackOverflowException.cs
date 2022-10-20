namespace MoonSharp.Interpreter
{
	public class StackOverflowException : ScriptRuntimeException
	{
		public StackOverflowException() : base("stack overflow") { }
	}
}
