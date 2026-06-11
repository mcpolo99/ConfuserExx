using System;
using System.Linq;
using System.Text.RegularExpressions;
using Confuser.Core;
using Confuser.Renamer.Properties;
using Confuser.Renamer.References;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;

namespace Confuser.Renamer.Analyzers {
	internal class ResourceAnalyzer : IRenamer {
		static readonly Regex ResourceNamePattern = new Regex("^(.*)\\.resources$");

		public void Analyze(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			var module = def as ModuleDef;
			if (module == null) return;

			string asmName = module.Assembly.Name.String;
			if (!string.IsNullOrEmpty(module.Assembly.Culture) &&
				asmName.EndsWith(".resources")) {
				// Satellite assembly
				string satellitePattern = $"^(.*)\\.(?i:({module.Assembly.Culture}))\\.resources$";
				string nameAsmName = asmName.Substring(0, asmName.Length - ".resources".Length);
				ModuleDef mainModule = context.Modules.SingleOrDefault(mod => mod.Assembly.Name == nameAsmName);
				if (mainModule == null) {
					context.Logger.LogError("Could not find main assembly of satellite assembly '{0}'.", module.Assembly.FullName);
					throw new ConfuserException(null);
				}

				foreach (Resource res in module.Resources) {
					Match match = Regex.Match(res.Name, satellitePattern);
					if (!match.Success) continue;
					string typeName = match.Groups[1].Value;
					string culture = match.Groups[2].Value;

					TypeDef type = mainModule.FindReflection(typeName);
					if (type == null) {
						context.Logger.LogWarning(Resources.ResourceAnalyzer_Analyze_CouldNotFindResourceType, typeName);
						continue;
					}
					string format = $"{{0}}.{culture}.resources";

					service.ReduceRenameMode(type, RenameMode.Reflection);
					service.AddReference(type, new ResourceReference(res, type, format));
				}
			}
			else {
				string format = "{0}.resources";
				foreach (Resource res in module.Resources) {
					Match match = ResourceNamePattern.Match(res.Name);
					if (!match.Success || res.ResourceType != ResourceType.Embedded)
						continue;
					string typeName = match.Groups[1].Value;

					if (typeName.EndsWith(".g")) // WPF resources, ignore
						continue;

					// This variable is set true in case the name of the resource doesn't match the name of the class.
					// That happens for the resources in Visual Basic.
					var mismatchingName = false;
					TypeDef type = module.FindReflection(typeName);
					if (type == null) {
						if (typeName.EndsWith(".Resources")) {
							typeName = typeName.Substring(0, typeName.Length - 10) + ".My.Resources.Resources";
							type = module.FindReflection(typeName);
							mismatchingName = type != null;
						}
					}

					if (type == null) {
						context.Logger.LogWarning(Resources.ResourceAnalyzer_Analyze_CouldNotFindResourceType, typeName);
						continue;
					}
					service.ReduceRenameMode(type, RenameMode.Reflection);
					service.AddReference(type, new ResourceReference(res, type, format));

					if (mismatchingName)
						// Add string type references in case the name didn't match. This will cause the resource to get
						// the same name as the class, despite that not being the case before. But that doesn't really matter.
						FindLdTokenResourceReferences(type, match.Groups[1].Value, service);
				}
			}
		}

		private static void FindLdTokenResourceReferences(TypeDef type, string name, INameService service) {
			foreach (var method in type.Methods)
				FindLdTokenResourceReferences(type, method, name, service);
		}

		private static void FindLdTokenResourceReferences(TypeDef type, MethodDef method, string name, INameService service) {
			if (!method.HasBody) return;

			foreach (var instr in method.Body.Instructions) {
				if (instr.OpCode.Code == Code.Ldstr && ((string)instr.Operand).Equals(name)) {
					service.AddReference(type, new StringTypeReference(instr, type));
				}
			}
		}

		public void PreRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			//
		}

		public void PostRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			//
		}
	}
}
