using System;
using System.Linq;
using Confuser.Core;
using Confuser.Core.Services;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Confuser.Renamer.Analyzers {
	public sealed class ManifestResourceAnalyzer : IRenamer {
		/// <inheritdoc />
		public void Analyze(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) { }

		/// <inheritdoc />
		void IRenamer.PreRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			if (!(def is MethodDef methodDef) || !methodDef.HasBody || !methodDef.Body.HasInstructions) return;

			var trace = context.Registry.GetService<ITraceService>();
			PreRename(context.CurrentModule, trace, methodDef);
		}

		public static void PreRename(ModuleDef currentModule, ITraceService trace, MethodDef methodDef) {
			var instructions = methodDef.Body.Instructions;
			var methodTrace = new Lazy<MethodTrace>(() => trace.Trace(methodDef));
			for (var i = 0; i < instructions.Count; i++) {
				var instruction = instructions[i];
				if (instruction.OpCode != OpCodes.Callvirt ||
					!(instruction.Operand is IMethodDefOrRef targetMethodDefOrRef) ||
					!UTF8String.Equals(targetMethodDefOrRef.Name, "GetManifestResourceStream") ||
					!UTF8String.Equals(targetMethodDefOrRef.DeclaringType.FullName, "System.Reflection.Assembly")) continue;

				// The two-argument overload GetManifestResourceStream(Type, String) has two
				// signature parameters (the instance 'this' is separate). Check the signature
				// directly so we don't depend on resolving the BCL method.
				var targetSig = targetMethodDefOrRef.MethodSig;
				if (targetSig == null || targetSig.Params.Count != 2) continue;

				var argumentIdx = methodTrace.Value.TraceArguments(instruction);
				if (argumentIdx.Length != 3) continue;

				var typeLoadInstruction = instructions[argumentIdx[1]];
				var resNameInstruction = instructions[argumentIdx[2]];

				if (typeLoadInstruction.OpCode != OpCodes.Call ||
					!(typeLoadInstruction.Operand is IMethodDefOrRef loadTypeMethodRef) ||
					!UTF8String.Equals(loadTypeMethodRef.Name, "GetTypeFromHandle") ||
					!UTF8String.Equals(loadTypeMethodRef.DeclaringType.FullName, "System.Type")) continue;
				if (resNameInstruction.OpCode != OpCodes.Ldstr ||
					!(resNameInstruction.Operand is string resName)) continue;

				var typeLoadArguments = methodTrace.Value.TraceArguments(typeLoadInstruction);
				if (typeLoadArguments.Length != 1) continue;

				var typeTokenLoadInstruction = instructions[typeLoadArguments[0]];
				if (typeTokenLoadInstruction.OpCode != OpCodes.Ldtoken ||
					!(typeTokenLoadInstruction.Operand is ITypeDefOrRef refTypeDefOrRef)) continue;

				var resourceName = refTypeDefOrRef.Namespace + '.' + resName;

				// Build the reference to the single-argument overload:
				// Stream System.Reflection.Assembly::GetManifestResourceStream(String).
				// Prefer resolving the real overload (matches production exactly); if the BCL
				// declaring type can't be resolved — e.g. in a minimal analysis context where the
				// runtime assemblies aren't on the resolver's search path — construct the member
				// reference directly from the original call's signature.
				var expectedSig = MethodSig.CreateInstance(targetSig.RetType, targetSig.Params[1]);
				var assemblyTypeDef = targetMethodDefOrRef.ResolveMethodDef()?.DeclaringType;
				IMethod newMethodRef;
				if (assemblyTypeDef != null) {
					var newMethodDef = assemblyTypeDef.FindMethod("GetManifestResourceStream", expectedSig);
					newMethodRef = currentModule.Import(newMethodDef);
				}
				else {
					newMethodRef = currentModule.Import(
						new MemberRefUser(currentModule, "GetManifestResourceStream", expectedSig, targetMethodDefOrRef.DeclaringType));
				}

				resNameInstruction.Operand = resourceName;
				instruction.Operand = newMethodRef;

				instructions.RemoveAt(argumentIdx[1]);
				instructions.RemoveAt(typeLoadArguments[0]);
			}
		}

		/// <inheritdoc />
		public void PostRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) { }
	}
}
