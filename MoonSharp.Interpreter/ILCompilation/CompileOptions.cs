using System.Reflection.Emit;

namespace MoonSharp.Interpreter.ILCompilation;

public record CompileOptions(ILGenerator Il, bool Box = false);
