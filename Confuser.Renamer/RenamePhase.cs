using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Confuser.Core;
using dnlib.DotNet;
using dnlib.DotNet.Pdb;
using Microsoft.Extensions.Logging;

namespace Confuser.Renamer {
	class RenamePhase : ProtectionPhase {
		public RenamePhase(NameProtection parent)
			: base(parent) { }

		public override ProtectionTargets Targets => ProtectionTargets.AllDefinitions;

		public override string Name => "Renaming";

		// Tracks overload confusion name assignments: TypeDef -> list of assigned names
		readonly Dictionary<TypeDef, List<string>> _overloadNames = new Dictionary<TypeDef, List<string>>();

		protected override void Execute(ConfuserContext context, ProtectionParameters parameters) {
			var service = (NameService)context.Registry.GetService<INameService>();
			bool overloadConfusion = parameters.GetParameter(context, context.CurrentModule, "overload", false);

			context.Logger.LogDebug("Renaming...");
			foreach (var renamer in service.Renamers) {
				foreach (var def in parameters.Targets)
					renamer.PreRename(context, service, parameters, def);
				context.CheckCancellation();
			}

			var targets = parameters.Targets.ToList();
			service.GetRandom().Shuffle(targets);
			var pdbDocs = new HashSet<string>();
			foreach (var def in GetTargetsWithDelay(targets, context, service).WithProgress(targets.Count, context.ProgressReporter)) {
				if (def is ModuleDef moduleDef && parameters.GetParameter(context, moduleDef, "rickroll", false))
					RickRoller.CommenceRickroll(context, moduleDef);

				bool canRename = service.CanRename(def);
				var mode = service.GetRenameMode(def);

				if (def is MethodDef method) {
					if ((canRename || method.IsConstructor) && parameters.GetParameter(context, method, "renameArgs", true)) {
						if (method.IsConstructor && method.DeclaringType.Name.String.Contains("AnonymousType")) {
							// Anonymous type constructor args map to property names — renaming breaks JSON serialization
						}
						else {
							foreach (var param in method.ParamDefs)
								param.Name = null;
						}
					}

					if (parameters.GetParameter(context, method, "renPdb", false) && method.HasBody) {
						foreach (var instr in method.Body.Instructions) {
							if (instr.SequencePoint != null && !pdbDocs.Contains(instr.SequencePoint.Document.Url)) {
								instr.SequencePoint.Document.Url = service.ObfuscateName(instr.SequencePoint.Document.Url, mode);
								pdbDocs.Add(instr.SequencePoint.Document.Url);
							}
						}
						foreach (var local in method.Body.Variables) {
							if (!string.IsNullOrEmpty(local.Name))
								local.Name = service.ObfuscateName(local.Name, mode);
						}

						if (method.Body.HasPdbMethod)
							method.Body.PdbMethod.Scope = new PdbScope();
					}
				}

				if (!canRename)
					continue;

				service.SetIsRenamed(def);

				var references = service.GetReferences(def);
				bool cancel = references.Any(r => r.ShouldCancelRename);
				if (cancel)
					continue;

				if (def is TypeDef typeDef) {
					if (parameters.GetParameter(context, typeDef, "flatten", true)) {
						typeDef.Namespace = "";
					}
					else {
						var nsFormat = parameters.GetParameter(context, typeDef, "nsFormat", "{0}");
						typeDef.Namespace = service.ObfuscateName(nsFormat, typeDef.Namespace, mode);
					}
					typeDef.Name = service.ObfuscateName(typeDef, mode);
					RenameGenericParameters(typeDef.GenericParameters);
				}
				else if (def is MethodDef methodDef) {
					if (overloadConfusion && CanApplyOverloadConfusion(methodDef, service)) {
						methodDef.Name = GetOverloadName(methodDef, service, mode);
					}
					else {
						methodDef.Name = service.ObfuscateName(methodDef, mode);
					}
					RenameGenericParameters(methodDef.GenericParameters);
				}
				else
					def.Name = service.ObfuscateName(def, mode);

				int updatedReferences = -1;
				do {
					var oldUpdatedCount = updatedReferences;
					// This resolves the changed name references and counts how many were changed.
					var updatedReferenceList = references.Where(refer => refer.UpdateNameReference(context, service)).ToArray();
					updatedReferences = updatedReferenceList.Length;
					if (updatedReferences == oldUpdatedCount) {
						var errorBuilder = new StringBuilder();
						errorBuilder.AppendLine("Infinite loop detected while resolving name references.");
						errorBuilder.Append("Processed definition: ").AppendDescription(def, service).AppendLine();
						errorBuilder.Append("Assembly: ").AppendLine(context.CurrentModule.FullName);
						errorBuilder.AppendLine("Faulty References:");
						foreach (var reference in updatedReferenceList) {
							errorBuilder.Append(" - ").AppendLine(reference.ToString(service));
						}
						context.Logger.LogError(errorBuilder.ToString().Trim());
						throw new ConfuserException();
					}
					context.CheckCancellation();
				} while (updatedReferences > 0);
			}
		}

		static void RenameGenericParameters(IList<GenericParam> genericParams) {
			foreach (var param in genericParams)
				param.Name = ((char)(param.Number + 1)).ToString();
		}

		/// <summary>
		///     Determines whether overload confusion can safely be applied to the method.
		///     Only applies to non-virtual, non-interface, non-special methods.
		/// </summary>
		static bool CanApplyOverloadConfusion(MethodDef method, INameService service) {
			if (method.IsVirtual || method.IsAbstract || method.IsConstructor)
				return false;
			if (method.IsSpecialName || method.IsRuntimeSpecialName)
				return false;
			// Skip if method has override/sibling references (VTable constrained)
			var refs = service.GetReferences(method);
			if (refs.Any(r => r.ShouldCancelRename || r.DelayRenaming(service, method)))
				return false;
			return true;
		}

		/// <summary>
		///     Gets a shared overload name for the method's declaring type.
		///     Methods with different signatures in the same type get the same name.
		/// </summary>
		string GetOverloadName(MethodDef method, NameService service, RenameMode mode) {
			var declType = method.DeclaringType;
			if (!_overloadNames.TryGetValue(declType, out var names)) {
				names = new List<string>();
				_overloadNames[declType] = names;
			}

			// Check if any existing name can be reused (different signature = safe to share)
			foreach (var existingName in names) {
				bool signatureConflict = declType.Methods.Any(m =>
					m.Name == existingName &&
					new SigComparer().Equals(m.MethodSig, method.MethodSig));
				if (!signatureConflict)
					return existingName;
			}

			// No reusable name — generate a new one
			var newName = service.ObfuscateName(method, mode);
			names.Add(newName);
			return newName;
		}

		static IEnumerable<IDnlibDef> GetTargetsWithDelay(IList<IDnlibDef> definitions, ConfuserContext context, INameService service) {
			var delayedItems = new List<IDnlibDef>();
			var currentList = definitions;
			var lastCount = -1;
			while (currentList.Any()) {
				foreach (var def in currentList) {
					if (service.GetReferences(def).Any(r => r.DelayRenaming(service, def)))
						delayedItems.Add(def);
					else
						yield return def;
				}

				if (delayedItems.Count == lastCount) {
					var errorBuilder = new StringBuilder();
					errorBuilder.AppendLine("Failed to rename all targeted members, because the references are blocking each other.");
					errorBuilder.AppendLine("Remaining definitions: ");
					foreach (var def in delayedItems) {
						errorBuilder.Append("• ").AppendDescription(def, service).AppendLine();
					}
					context.Logger.LogWarning(errorBuilder.ToString().Trim());
					yield break;
				}
				lastCount = delayedItems.Count;
				currentList = delayedItems;
				delayedItems = new List<IDnlibDef>();
			}
		}
	}
}
