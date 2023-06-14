using System.Collections.Generic;
using System.Reflection.Emit;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.ILCompilation;

namespace MoonSharp.Interpreter.Tree.Expressions
{
	class ExprListExpression : Expression 
	{
		List<Expression> expressions;

		public ExprListExpression(List<Expression> exps, ScriptLoadingContext lcontext)
			: base(lcontext)
		{
			expressions = exps;
		}


		public Expression[] GetExpressions()
		{
			return expressions.ToArray();
		}

		public override void Compile(Execution.VM.ByteCode bc)
		{
			foreach (var exp in expressions)
				exp.Compile(bc);

			if (expressions.Count > 1)
				bc.Emit_MkTuple(expressions.Count);
		}

		protected override ILType GetIlType()
		{
			// TODO
			return expressions[0].Type;
		}

		public override void CompileIl(CompileOptions compileOptions) {
			foreach (var exp in expressions)
				exp.CompileIl(compileOptions);

			//if (expressions.Count > 1)
				//il.Emit_MkTuple(expressions.Count);
		}

		public override DynValue Eval(ScriptExecutionContext context)
		{
			if (expressions.Count >= 1)
				return expressions[0].Eval(context);

			return DynValue.Void;
		}
	}
}
