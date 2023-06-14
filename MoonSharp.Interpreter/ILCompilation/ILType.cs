using System;
using System.Reflection.Emit;

namespace MoonSharp.Interpreter.ILCompilation;

[Flags]
public enum ILType {
	Nil = 0,
	Boolean = 1 << 0,
	Number = 1 << 2,
	String = 1 << 3,
	Function = 1 << 4,
	Userdata = 1 << 5,
	Thread = 1 << 6,
	Table = 1 << 7,
}

public static class Extensions
{
	public static bool IsSingle(this ILType type)
	{
		return ((int)type & ((int)type - 1)) == 0;
	}

	public static void EmitBox(this ILType type, ILGenerator il)
	{
		il.Emit(OpCodes.Box, type switch
		{
			ILType.Nil => typeof(object),
			ILType.Boolean => typeof(bool),
			ILType.Number => typeof(double),
			ILType.String => typeof(string),
			// TODO: No idea
			ILType.Function => typeof(Closure),
			ILType.Userdata => typeof(UserData),
			ILType.Thread => typeof(ScriptExecutionContext),
			ILType.Table => typeof(Table),
			_ => throw new InvalidOperationException(),
		});
	}
}
