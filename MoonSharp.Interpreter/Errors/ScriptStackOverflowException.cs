namespace MoonSharp.Interpreter
{
	public class ScriptStackOverflowException : ScriptRuntimeException
	{
		public ScriptStackOverflowException() : base("stack overflow") { }
	}
}
