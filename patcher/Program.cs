using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;
using System;
using System.Text.RegularExpressions;
using FRML;
using System.IO;
using System.Collections.Generic;

namespace patcher {
	public static class Program {
		// TODO: add more to ignore
		// TODO: add ignore methods too
		private static Regex[] ignoreClasses = {
			new Regex(@"^Tuple`\d$"),
			new Regex(@"^EntryPoint$"),
			new Regex(@"^InputManager$"),
			new Regex(@"^ByteReader$"),
			new Regex(@"^EncodedDictionary`\d$"),
			new Regex(@"^MathUtils$"),
			new Regex(@"^Slerp$"),
			new Regex(@"^Rotate$"),
			new Regex(@"^SplineInterpolator$")
		};

		private static Regex[] ignoreMethods = {
			new Regex(@"^get_"),
			new Regex(@"^set_"),
			new Regex(@"^.ctor$")
		};

		static void Main(string[] args) {
			string fileName = "Assembly-CSharp.dll";

			try {
				File.Move(fileName, fileName + ".bak");
				Console.WriteLine("Created backup: " + fileName + ".bak");
			}
			catch (FileNotFoundException) {
				Console.WriteLine("Assembly-CSharp.dll not found! Is this in the right directory?");
				return;
			}
			catch (IOException) {
				Console.WriteLine("Can't backup Assembly-CSharp.dll");
				Console.WriteLine("Please rename Assembly-CSharp.dll.bak before running.");
				return;
			}

			ModuleDefinition module = ModuleDefinition.ReadModule(fileName + ".bak");

			if (module.GetType("FRMLInstalled") != null) {
				Console.WriteLine("Error: Assembly-CSharp.dll already patched!");
				Console.WriteLine("FRML is already installed!");
				return;
			}

			Console.WriteLine("Patching " + fileName + "...");

			// just an empty class to note that FRML is installed
			TypeDefinition t = new TypeDefinition("", "FRMLInstalled", Mono.Cecil.TypeAttributes.Public, module.ImportReference(typeof(object)));
			module.Types.Add(t);

			MethodInfo callMethod = typeof(ModLoader).GetMethod("Call");

			foreach (TypeDefinition type in module.Types) {
				List<String> callbacks = new List<String>();

				// don't use for: classes outside of global namespace or structs
				if (!(type.Namespace != "" || (type.IsValueType && !type.IsPrimitive && !type.IsEnum))) {
					bool dontuse = false;

					foreach (Regex i in ignoreClasses) {
						if (i.IsMatch(type.Name)) {
							dontuse = true;
							break;
						}
					}

					if (!dontuse) {
						foreach (MethodDefinition m in type.Methods) {
							dontuse = false;

							foreach (Regex i in ignoreMethods) {
								if (i.IsMatch(m.Name)) {
									dontuse = true;
									break;
								}
							}

							// TODO: support references
							// would need different il code for different ref types
							foreach (var p in m.Parameters) {
								if (p.ParameterType.IsByReference) {
									dontuse = true;
									break;
								}
							}

							if (!dontuse && m.Body != null) {
								callbacks.Add(m.Name);

								UInt64 hashCode = ModLoader.HashCode(type.Name, m.Name);
								//Console.WriteLine("Patching " + type.Name + "::" + m.Name + "  code=" + hashCode);

								MethodReference call = module.ImportReference(callMethod);
								ILProcessor p = m.Body.GetILProcessor();

								Instruction iCall = p.Create(OpCodes.Call, call);
								p.InsertBefore(m.Body.Instructions[0], iCall);

								p.InsertBefore(iCall, p.Create(OpCodes.Ldc_I8, (long)hashCode));

								if (m.IsStatic) {
									// push null (static method has no this)
									p.InsertBefore(iCall, p.Create(OpCodes.Ldnull));
								}
								else {
									// push this
									p.InsertBefore(iCall, p.Create(OpCodes.Ldarg_0));
								}

								// create params array
								p.InsertBefore(iCall, p.Create(OpCodes.Ldc_I4, m.Parameters.Count));
								p.InsertBefore(iCall, p.Create(OpCodes.Newarr, module.ImportReference(typeof(Object))));

								for (int i = 0; i < m.Parameters.Count; ++i) {
									p.InsertBefore(iCall, p.Create(OpCodes.Dup));
									p.InsertBefore(iCall, p.Create(OpCodes.Ldc_I4, i));
									p.InsertBefore(iCall, p.Create(OpCodes.Ldarg, m.Parameters[i]));
									p.InsertBefore(iCall, p.Create(OpCodes.Box, m.Parameters[i].ParameterType));
									p.InsertBefore(iCall, p.Create(OpCodes.Stelem_Ref));
								}
							}
						}
					}
				}

				Documentation.CreateClass(type, callbacks);
			}

			Documentation.SaveIndex();

			Console.WriteLine("Patch complete.");
			module.Write(fileName);
		}
	}
}
