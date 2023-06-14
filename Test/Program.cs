// See https://aka.ms/new-console-template for more information

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Tree.Statements;
using System.Reflection.Emit;
using System.Reflection;

var script = new Script();
var source = new SourceCode("test", "return true ? 1 : 'test'", 0, script);
var loading = new ScriptLoadingContext(script)
{
	Scope = new(),
	Source = source,
	Lexer = new(source.SourceID, source.Code, true),
};
var statement = new ChunkStatement(loading);

var asm = AssemblyBuilder.DefineDynamicAssembly(new("aa"), AssemblyBuilderAccess.RunAndCollect);
var mb = asm.DefineDynamicModule("test");
var method = mb.DefineGlobalMethod("test", MethodAttributes.Public | MethodAttributes.Static, typeof(object),
	Type.EmptyTypes);
var il = method.GetILGenerator();
statement.CompileIl(new(il));

mb.CreateGlobalFunctions();

Console.WriteLine(mb.GetMethod("test").Invoke(null, null));

var test = Script.RunString("return 2");

int i = 0;

typeof(A).GetMethods().ToList().ForEach(x => {
	Console.WriteLine($"{i++} {x.ReturnType} {x.Name}");
});

dynamic test5 = Random.Shared.Next(2) == 1 ? 2.0 : 2;

class A
{
	public dynamic Test() {
		return 2;
	}
}
