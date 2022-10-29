using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter.Compatibility;
using MoonSharp.Interpreter.Interop.BasicDescriptors;
using MoonSharp.Interpreter.Interop.Converters;

namespace MoonSharp.Interpreter.Interop
{
	/// <summary>
	/// Class providing easier marshalling of CLR functions
	/// </summary>
	public class GenericMethodMemberDescriptor : FunctionMemberDescriptorBase, IWireableDescriptor
	{
		/// <summary>
		/// Gets the method information (can be a MethodInfo or ConstructorInfo)
		/// </summary>
		public MethodBase MethodInfo { get; private set; }
		/// <summary>
		/// Gets the access mode used for interop
		/// </summary>
		public InteropAccessMode AccessMode { get; private set; }
		/// <summary>
		/// Gets a value indicating whether the described method is a constructor
		/// </summary>
		public bool IsConstructor { get; private set; }

		private bool m_IsAction = false;
		private bool m_IsArrayCtor = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodMemberDescriptor"/> class.
		/// </summary>
		/// <param name="methodBase">The MethodBase (MethodInfo or ConstructorInfo) got through reflection.</param>
		/// <param name="accessMode">The interop access mode.</param>
		/// <exception cref="System.ArgumentException">Invalid accessMode</exception>
		public GenericMethodMemberDescriptor(MethodBase methodBase, InteropAccessMode accessMode = InteropAccessMode.Default)
		{
			CheckMethodIsCompatible(methodBase, true);

			IsConstructor = (methodBase is ConstructorInfo);
			this.MethodInfo = methodBase;

			bool isStatic = methodBase.IsStatic || IsConstructor;

			if (IsConstructor)
				m_IsAction = false;
			else
				m_IsAction = ((MethodInfo)methodBase).ReturnType == typeof(void);

			ParameterInfo[] reflectionParams = methodBase.GetParameters();
			ParameterDescriptor[] parameters;

			if (this.MethodInfo.DeclaringType.IsArray)
			{
				m_IsArrayCtor = true;

				int rank = this.MethodInfo.DeclaringType.GetArrayRank();

				parameters = new ParameterDescriptor[rank];

				for (int i = 0; i < rank; i++)
					parameters[i] = new ParameterDescriptor("idx" + i, typeof(int));
			}
			else
			{
				if (IsConstructor)
				{
					parameters = reflectionParams.Select(pi => new ParameterDescriptor(pi)).ToArray();
				}
				else
				{
					int genericCount = ((MethodInfo)MethodInfo).GetGenericArguments().Length;
					parameters = new ParameterDescriptor[reflectionParams.Length + genericCount];

					for (int i = 0; i < genericCount; i++)
					{
						parameters[i] = new ParameterDescriptor("generic" + i, typeof(object));
					}

					for (int i = genericCount; i < parameters.Length; i++)
					{
						parameters[i] = new ParameterDescriptor(reflectionParams[i - genericCount]);
					}
				}
			}


			bool isExtensionMethod = (methodBase.IsStatic && parameters.Length > 0 && methodBase.GetCustomAttributes(typeof(ExtensionAttribute), false).Any());

			base.Initialize(methodBase.Name, isStatic, parameters, isExtensionMethod);

			// adjust access mode
			if (Script.GlobalOptions.Platform.IsRunningOnAOT())
				accessMode = InteropAccessMode.Reflection;

			if (accessMode == InteropAccessMode.Default)
				accessMode = UserData.DefaultAccessMode;

			if (accessMode == InteropAccessMode.HideMembers)
				throw new ArgumentException("Invalid accessMode");

			if (parameters.Any(p => p.Type.IsByRef))
				accessMode = InteropAccessMode.Reflection;

			this.AccessMode = accessMode;

			if (AccessMode == InteropAccessMode.Preoptimized)
				((IOptimizableDescriptor)this).Optimize();
		}

		/// <summary>
		/// Tries to create a new MethodMemberDescriptor, returning 
		/// <c>null</c> in case the method is not
		/// visible to script code.
		/// </summary>
		/// <param name="methodBase">The MethodBase.</param>
		/// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
		/// <param name="forceVisibility">if set to <c>true</c> forces visibility.</param>
		/// <returns>
		/// A new MethodMemberDescriptor or null.
		/// </returns>
		public static GenericMethodMemberDescriptor TryCreateIfVisible(MethodBase methodBase, InteropAccessMode accessMode, bool forceVisibility = false)
		{
			if (!CheckMethodIsCompatible(methodBase, false))
				return null;

			if (forceVisibility || (methodBase.GetVisibilityFromAttributes() ?? methodBase.IsPublic))
				return new GenericMethodMemberDescriptor(methodBase, accessMode);

			return null;
		}

		/// <summary>
		/// Checks if the method is compatible with a standard descriptor
		/// </summary>
		/// <param name="methodBase">The MethodBase.</param>
		/// <param name="throwException">if set to <c>true</c> an exception with the proper error message is thrown if not compatible.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentException">
		/// Thrown if throwException is <c>true</c> and one of this applies:
		/// The method contains unresolved generic parameters, or has an unresolved generic return type
		/// or
		/// The method contains pointer parameters, or has a pointer return type
		/// </exception>
		public static bool CheckMethodIsCompatible(MethodBase methodBase, bool throwException)
		{
			if (!methodBase.ContainsGenericParameters)
			{
				if (throwException) throw new ArgumentException("Method doesn't contain unresolved generic parameters");
				return false;
			}

			if (methodBase.GetParameters().Any(p => p.ParameterType.IsPointer))
			{
				if (throwException) throw new ArgumentException("Method cannot contain pointer parameters");
				return false;
			}

			MethodInfo mi = methodBase as MethodInfo;

			if (mi != null)
			{
				if (mi.ReturnType.IsPointer)
				{
					if (throwException) throw new ArgumentException("Method cannot have a pointer return type");
					return false;
				}

				if (Framework.Do.IsGenericTypeDefinition(mi.ReturnType))
				{
					//if (throwException) throw new ArgumentException("Method cannot have an unresolved generic return type");
					//return false;
				}
			}

			return true;
		}

		/// <summary>
		/// The internal callback which actually executes the method
		/// </summary>
		/// <param name="script">The script.</param>
		/// <param name="obj">The object.</param>
		/// <param name="context">The context.</param>
		/// <param name="args">The arguments.</param>
		/// <returns></returns>
		public override DynValue Execute(Script script, object obj, ScriptExecutionContext context, CallbackArguments args)
		{
			this.CheckAccess(MemberDescriptorAccess.CanExecute, obj);

			List<int> outParams = null;
			object[] pars = BuildArgumentList(script, obj, context, args, out outParams);
			object retv = null;

			int amountGenerics = MethodInfo.GetGenericArguments().Length;

			Type[] generics = new Type[amountGenerics];
			object[] parameters = new object[pars.Length - amountGenerics];

			for (var i = 0; i < amountGenerics; i++)
			{
				if (pars[i] == null)
				{
					throw new ScriptRuntimeException("Tried to call a generic method without a generic argument.");
				}

				if (pars[i] is Type type)
				{
					generics[i] = type;
				}
				else
				{
					generics[i] = pars[i].GetType();
				}
			}

			for (var i = 0; i < parameters.Length; i++)
			{
				parameters[i] = pars[i + amountGenerics];
			}

			MethodInfo genericMethod = ((MethodInfo)MethodInfo).MakeGenericMethod(generics);
			retv = genericMethod.Invoke(obj, parameters);


			return BuildReturnValue(script, outParams, pars, retv);
		}


		/// <summary>
		/// Builds the argument list.
		/// </summary>
		/// <param name="script">The script.</param>
		/// <param name="obj">The object.</param>
		/// <param name="context">The context.</param>
		/// <param name="args">The arguments.</param>
		/// <param name="outParams">Output: A list containing the indices of all "out" parameters, or null if no out parameters are specified.</param>
		/// <returns>The arguments, appropriately converted.</returns>
		protected override object[] BuildArgumentList(Script script, object obj, ScriptExecutionContext context, CallbackArguments args,
			out List<int> outParams)
		{
			ParameterDescriptor[] parameters = Parameters;

			object[] pars = new object[parameters.Length];

			int j = args.IsMethodCall ? 1 : 0;

			outParams = null;

			for (int i = 0; i < args.Count; i++)
			{
				// keep track of out and ref params
				if (parameters[i].Type.IsByRef)
				{
					if (outParams == null) outParams = new List<int>();
					outParams.Add(i);
				}

				// if an ext method, we have an obj -> fill the first param
				if (ExtensionMethodType != null && obj != null && i == 0)
				{
					pars[i] = obj;
					continue;
				}
				// else, fill types with a supported type
				else if (parameters[i].Type == typeof(Script))
				{
					pars[i] = script;
				}
				else if (parameters[i].Type == typeof(ScriptExecutionContext))
				{
					pars[i] = context;
				}
				else if (parameters[i].Type == typeof(CallbackArguments))
				{
					pars[i] = args.SkipMethodCall();
				}
				// else, ignore out params
				else if (parameters[i].IsOut)
				{
					pars[i] = null;
				}
				else if (i == parameters.Length - 1 && VarArgsArrayType != null)
				{
					List<DynValue> extraArgs = new List<DynValue>();

					while (true)
					{
						DynValue arg = args.RawGet(j, false);
						j += 1;
						if (arg != null)
							extraArgs.Add(arg);
						else
							break;
					}

					// here we have to worry we already have an array.. damn. We only support this for userdata.
					// remains to be analyzed what's the correct behavior here. For example, let's take a params object[]..
					// given a single table parameter, should it use it as an array or as an object itself ?
					if (extraArgs.Count == 1)
					{
						DynValue arg = extraArgs[0];

						if (arg.Type == DataType.UserData && arg.UserData.Object != null)
						{
							if (Framework.Do.IsAssignableFrom(VarArgsArrayType, arg.UserData.Object.GetType()))
							{
								pars[i] = arg.UserData.Object;
								continue;
							}
						}
					}

					// ok let's create an array, and loop
					Array vararg = Array.CreateInstance(VarArgsElementType, extraArgs.Count);

					for (int ii = 0; ii < extraArgs.Count; ii++)
					{
						vararg.SetValue(ScriptToClrConversions.DynValueToObjectOfType(extraArgs[ii], VarArgsElementType,
						null, false), ii);
					}

					pars[i] = vararg;

				}
				// else, convert it
				else
				{
					var arg = args.RawGet(j, false) ?? DynValue.Void;
					pars[i] = ScriptToClrConversions.DynValueToObjectOfType(arg, parameters[i].Type,
						parameters[i].DefaultValue, parameters[i].HasDefaultValue);
					j += 1;
				}
			}

			return pars;
		}

		/// <summary>
		/// Prepares the descriptor for hard-wiring.
		/// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
		/// </summary>
		/// <param name="t">The table to be filled</param>
		public void PrepareForWiring(Table t)
		{
			t.Set("class", DynValue.NewString(this.GetType().FullName));
			t.Set("name", DynValue.NewString(this.Name));
			t.Set("ctor", DynValue.NewBoolean(this.IsConstructor));
			t.Set("special", DynValue.NewBoolean(this.MethodInfo.IsSpecialName));
			t.Set("visibility", DynValue.NewString(this.MethodInfo.GetClrVisibility()));

			if (this.IsConstructor)
				t.Set("ret", DynValue.NewString(((ConstructorInfo)this.MethodInfo).DeclaringType.FullName));
			else
				t.Set("ret", DynValue.NewString(((MethodInfo)this.MethodInfo).ReturnType.FullName));

			if (m_IsArrayCtor)
			{
				t.Set("arraytype", DynValue.NewString(this.MethodInfo.DeclaringType.GetElementType().FullName));
			}

			t.Set("decltype", DynValue.NewString(this.MethodInfo.DeclaringType.FullName));
			t.Set("static", DynValue.NewBoolean(this.IsStatic));
			t.Set("extension", DynValue.NewBoolean(this.ExtensionMethodType != null));

			var pars = DynValue.NewPrimeTable();

			t.Set("params", pars);

			int i = 0;

			foreach (var p in Parameters)
			{
				DynValue pt = DynValue.NewPrimeTable();
				pars.Table.Set(++i, pt);
				p.PrepareForWiring(pt.Table);
			}
		}
	}
}
