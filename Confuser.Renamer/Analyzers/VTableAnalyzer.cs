using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Confuser.Core;
using Confuser.Renamer.References;
using dnlib.DotNet;

namespace Confuser.Renamer.Analyzers {
	public class VTableAnalyzer : IRenamer {
		void IRenamer.Analyze(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			switch (def) {
				case TypeDef typeDef:
					Analyze(service, context.Modules, typeDef);
					break;
				case MethodDef methodDef:
					Analyze(service, context.Modules, methodDef);
					break;
			}
		}

		public static void Analyze(INameService service, ICollection<ModuleDefMD> modules, TypeDef type) {
			if (type.IsInterface)
				return;

			var vTbl = service.GetVTables()[type];
			foreach (var ifaceVTbl in vTbl.InterfaceSlots.Values) {
				foreach (var slot in ifaceVTbl) {
					if (slot.Overrides == null)
						continue;
					Debug.Assert(slot.Overrides.MethodDef.DeclaringType.IsInterface);
					// A method in base type can implements an interface method for a
					// derived type. If the base type/interface is not in our control, we should
					// not rename the methods.
					bool baseUnderCtrl = modules.Contains(slot.MethodDef.DeclaringType.Module as ModuleDefMD);
					bool interfaceUnderCtrl = modules.Contains(slot.Overrides.MethodDef.DeclaringType.Module as ModuleDefMD);
					if (!baseUnderCtrl && interfaceUnderCtrl || !service.CanRename(slot.MethodDef)) {
						service.SetCanRename(slot.Overrides.MethodDef, false);
					}
					else if ((baseUnderCtrl && !interfaceUnderCtrl) || (!service.CanRename(slot.Overrides.MethodDef))) {
						service.SetCanRename(slot.MethodDef, false);
					}

					// Now it is possible that the method implementing the interface, belongs to the base class.
					// If that happens the methods analyzing the methods will not pick up on this. We'll mark that
					// case here.
					if (!TypeEqualityComparer.Instance.Equals(slot.MethodDef.DeclaringType, type)) {
						SetupOverwriteReferences(service, modules, slot, type);

						// If required, create the sibling references, so the names of the interfaces line up correctly.
						var existingReferences = service.GetReferences(slot.MethodDef);
						var overrideDef = existingReferences
							.OfType<MemberOverrideReference>()
							.FirstOrDefault(r => !MethodEqualityComparer.CompareDeclaringTypes.Equals(r.BaseMemberDef as MethodDef, slot.Overrides.MethodDef));

						if (!(overrideDef is null)) {
							var baseMemberDef = overrideDef.BaseMemberDef;
							CreateSiblingReference(slot.Overrides.MethodDef, ref baseMemberDef, service);
						}
					}

					// For the case when method in base type implements an interface method for a derived type
					// do not consider method parameters to make method name the same in base type, derived type and interface
					var methodDef = slot.MethodDef;
					var typeDef = type.BaseType?.ResolveTypeDef();
					var baseMethod = typeDef?.FindMethod(methodDef.Name, methodDef.Signature as MethodSig);
					if (baseMethod != null) {
						string unifiedName = service.GetNormalizedName(slot.Overrides.MethodDef);
						service.SetNormalizedName(slot.MethodDef, unifiedName);
						service.SetNormalizedName(baseMethod, unifiedName);
					}
				}
			}
		}

		public static void Analyze(INameService service, ICollection<ModuleDefMD> modules, MethodDef method) {
			if (!method.IsVirtual)
				return;

			IMemberDef discoveredBaseMemberDef = null;
			MethodDef discoveredBaseMethodDef = null;

			bool doesOverridePropertyOrEvent = false;
			var methodProp = method.DeclaringType.Properties.Where(p => BelongsToProperty(p, method));
			foreach (var prop in methodProp) {
				foreach (var baseMethodDef in FindBaseDeclarations(service, method)) {
					var basePropDef = baseMethodDef.DeclaringType.Properties.
						FirstOrDefault(p => BelongsToProperty(p, baseMethodDef) && String.Equals(p.Name, prop.Name, StringComparison.Ordinal));

					if (basePropDef is null) continue;

					// Name of property has to line up.
					CreateOverrideReference(service, prop, basePropDef);
					CreateSiblingReference(basePropDef, ref discoveredBaseMemberDef, service);

					// Method names have to line up as well (otherwise inheriting attributes does not work).
					CreateOverrideReference(service, method, baseMethodDef);
					CreateSiblingReference(baseMethodDef, ref discoveredBaseMethodDef, service);

					doesOverridePropertyOrEvent = true;
				}
			}

			discoveredBaseMemberDef = null;
			discoveredBaseMethodDef = null;

			var methodEvent = method.DeclaringType.Events.Where(e => BelongsToEvent(e, method));
			foreach (var evt in methodEvent) {
				foreach (var baseMethodDef in FindBaseDeclarations(service, method)) {
					var baseEventDef = baseMethodDef.DeclaringType.Events.
						FirstOrDefault(e => BelongsToEvent(e, baseMethodDef) && String.Equals(e.Name, evt.Name, StringComparison.Ordinal));

					if (baseEventDef is null) continue;

					// Name of event has to line up.
					CreateOverrideReference(service, evt, baseEventDef);
					CreateSiblingReference(baseEventDef, ref discoveredBaseMemberDef, service);

					// Method names have to line up as well (otherwise inheriting attributes does not work).
					CreateOverrideReference(service, method, baseMethodDef);
					CreateSiblingReference(baseMethodDef, ref discoveredBaseMethodDef, service);

					doesOverridePropertyOrEvent = true;
				}
			}

			if (!method.IsAbstract) {
				var vTbl = service.GetVTables()[method.DeclaringType];
				var slots = vTbl.FindSlots(method).ToArray();

				foreach (var slot in slots) {
					if (slot.Overrides == null)
						continue;

					SetupOverwriteReferences(service, modules, slot, method.DeclaringType);
				}
			}
			else if (!doesOverridePropertyOrEvent) {
				foreach (var baseMethodDef in FindBaseDeclarations(service, method)) {
					CreateOverrideReference(service, method, baseMethodDef);
				}
			}
		}

		static void CreateSiblingReference<T>(T baseMemberDef, ref T discoveredBaseMemberDef, INameService service) where T : class, IMemberDef {
			if (discoveredBaseMemberDef is null)
				discoveredBaseMemberDef = baseMemberDef;
			else {
				var references = service.GetReferences(discoveredBaseMemberDef)
					.OfType<MemberSiblingReference>()
					.ToArray();
				if (references.Length > 0) {
					discoveredBaseMemberDef = (T)references[0].OldestSiblingDef;
					foreach (var siblingRef in references.Skip(1)) {
						// Redirect all the siblings to the new oldest reference
						RedirectSiblingReferences(siblingRef.OldestSiblingDef, discoveredBaseMemberDef, service);
					}
				}

				// Check if the discovered base type is the current type. If so, nothing needs to be done.
				if (ReferenceEquals(baseMemberDef, discoveredBaseMemberDef)) return;

				var reference = new MemberSiblingReference(baseMemberDef, discoveredBaseMemberDef);
				service.AddReference(baseMemberDef, reference);
				service.AddReference(discoveredBaseMemberDef, reference);
				UpdateOldestSiblingReference(discoveredBaseMemberDef, baseMemberDef, service);
			}
		}

		static void UpdateOldestSiblingReference(IMemberDef oldestSiblingMemberDef, IMemberDef basePropDef, INameService service) {
			var reverseReference = service.GetReferences(oldestSiblingMemberDef).OfType<MemberOldestSiblingReference>()
				.SingleOrDefault();
			if (reverseReference is null) {
				service.AddReference(oldestSiblingMemberDef, new MemberOldestSiblingReference(oldestSiblingMemberDef, basePropDef));
				PropagateRenamingRestrictions(service, oldestSiblingMemberDef, basePropDef);
			}
			else if (!reverseReference.OtherSiblings.Contains(basePropDef)) {
				reverseReference.OtherSiblings.Add(basePropDef);
				PropagateRenamingRestrictions(service, reverseReference.OtherSiblings);
			}
		}

		static void RedirectSiblingReferences(IMemberDef oldMemberDef, IMemberDef newMemberDef, INameService service) {
			if (ReferenceEquals(oldMemberDef, newMemberDef)) return;

			var referencesToUpdate = service.GetReferences(oldMemberDef)
				.OfType<MemberOldestSiblingReference>()
				.SelectMany(r => r.OtherSiblings)
				.SelectMany(service.GetReferences)
				.OfType<MemberSiblingReference>()
				.Where(r => ReferenceEquals(r.OldestSiblingDef, oldMemberDef));

			foreach (var reference in referencesToUpdate) {
				reference.OldestSiblingDef = newMemberDef;
				UpdateOldestSiblingReference(newMemberDef, reference.ThisMemberDef, service);
			}
			UpdateOldestSiblingReference(newMemberDef, oldMemberDef, service);
		}

		static void CreateOverrideReference(INameService service, IMemberDef thisMemberDef, IMemberDef baseMemberDef) {
			var overrideRef = new MemberOverrideReference(thisMemberDef, baseMemberDef);
			service.AddReference(thisMemberDef, overrideRef);
			service.AddReference(baseMemberDef, overrideRef);

			PropagateRenamingRestrictions(service, thisMemberDef, baseMemberDef);
		}

		static void PropagateRenamingRestrictions(INameService service, params object[] objects) =>
			PropagateRenamingRestrictions(service, (IList<object>)objects);

		static void PropagateRenamingRestrictions(INameService service, IList<object> objects) {
			if (!objects.All(service.CanRename)) {
				foreach (var o in objects) {
					service.SetCanRename(o, false);
				}
			}
			else {
				var minimalRenamingLevel = objects.Max(service.GetRenameMode);
				foreach (var o in objects) {
					service.ReduceRenameMode(o, minimalRenamingLevel);
				}
			}
		}

		private static IEnumerable<MethodDef> FindBaseDeclarations(INameService service, MethodDef method) {
			var unprocessed = new Queue<MethodDef>();
			unprocessed.Enqueue(method);

			var vTables = service.GetVTables();

			while (unprocessed.Any()) {
				var currentMethod = unprocessed.Dequeue();

				var vTbl = vTables[currentMethod.DeclaringType];
				var slots = vTbl.FindSlots(currentMethod).Where(s => s.Overrides != null);

				bool slotsExists = false;
				foreach (var slot in slots) {
					unprocessed.Enqueue(slot.Overrides.MethodDef);
					slotsExists = true;
				}

				if (!slotsExists && method != currentMethod)
					yield return currentMethod;
			}
		}

		private static bool BelongsToProperty(PropertyDef propertyDef, MethodDef methodDef) =>
			propertyDef.GetMethods.Contains(methodDef) || propertyDef.SetMethods.Contains(methodDef) ||
			(propertyDef.HasOtherMethods && propertyDef.OtherMethods.Contains(methodDef));

		private static bool BelongsToEvent(EventDef eventDef, MethodDef methodDef) =>
			Equals(eventDef.AddMethod, methodDef) || Equals(eventDef.RemoveMethod, methodDef) || Equals(eventDef.InvokeMethod, methodDef) ||
			(eventDef.HasOtherMethods && eventDef.OtherMethods.Contains(methodDef));

		private static void AddImportReference(INameService service, ICollection<ModuleDefMD> modules, ModuleDef module, MethodDef method, MemberRef methodRef) {
			if (method.Module != module && modules.Contains((ModuleDefMD)module)) {
				var declType = (TypeRef)methodRef.DeclaringType.ScopeType;
				service.AddReference(method.DeclaringType, new TypeRefReference(declType, method.DeclaringType));
				service.AddReference(method, new MemberRefReference(methodRef, method));

				var typeRefs = methodRef.MethodSig.Params.SelectMany(param => param.FindTypeRefs()).ToList();
				typeRefs.AddRange(methodRef.MethodSig.RetType.FindTypeRefs());
				typeRefs.AddRange(methodRef.DeclaringType.ToTypeSig().FindTypeRefs());
				foreach (var typeRef in typeRefs) {
					SetupTypeReference(service, modules, module, typeRef);
				}
			}
		}

		private static void SetupTypeReference(INameService service, ICollection<ModuleDefMD> modules, ModuleDef module, ITypeDefOrRef typeDefOrRef) {
			if (!(typeDefOrRef is TypeRef typeRef)) return;

			var def = typeRef.ResolveTypeDef();
			if (!(def is null) && def.Module != module && modules.Contains((ModuleDefMD)def.Module))
				service.AddReference(def, new TypeRefReference(typeRef, def));
		}

		private static void SetupSignatureReferences(INameService service, ICollection<ModuleDefMD> modules,
			ModuleDef module, GenericInstSig typeSig) {
			SetupSignatureReferences(service, modules, module, typeSig.GenericType);
			foreach (var genericArgument in typeSig.GenericArguments)
				SetupSignatureReferences(service, modules, module, genericArgument);
		}

		private static void SetupSignatureReferences(INameService service, ICollection<ModuleDefMD> modules, ModuleDef module, TypeSig typeSig) {
			var asTypeRef = typeSig.TryGetTypeRef();
			if (asTypeRef != null) {
				SetupTypeReference(service, modules, module, asTypeRef);
			}
		}

		private static void SetupOverwriteReferences(INameService service, ICollection<ModuleDefMD> modules, VTableSlot slot, TypeDef thisType) {
			var module = thisType.Module;
			var methodDef = slot.MethodDef;
			var baseSlot = slot.Overrides;
			var baseMethodDef = baseSlot.MethodDef;

			var overrideRef = new OverrideDirectiveReference(slot, baseSlot);
			service.AddReference(methodDef, overrideRef);
			service.AddReference(slot.Overrides.MethodDef, overrideRef);

			var importer = new Importer(module, ImporterOptions.TryToUseTypeDefs);

			IMethodDefOrRef target;
			if (baseSlot.MethodDefDeclType is GenericInstSig declType) {
				MemberRef targetRef = new MemberRefUser(module, baseMethodDef.Name, baseMethodDef.MethodSig, declType.ToTypeDefOrRef());
				targetRef = importer.Import(targetRef);
				service.AddReference(baseMethodDef, new MemberRefReference(targetRef, baseMethodDef));
				SetupSignatureReferences(service, modules, module, targetRef.DeclaringType.ToTypeSig() as GenericInstSig);

				target = targetRef;
			}
			else {
				target = baseMethodDef;
				if (target.Module != module) {
					target = (IMethodDefOrRef)importer.Import(baseMethodDef);
					if (target is MemberRef memberRef)
						service.AddReference(baseMethodDef, new MemberRefReference(memberRef, baseMethodDef));
				}
			}

			if (target is MemberRef methodRef)
				AddImportReference(service, modules, module, baseMethodDef, methodRef);

			if (TypeEqualityComparer.Instance.Equals(methodDef.DeclaringType, thisType)) {
				if (methodDef.Overrides.Any(impl => IsMatchingOverride(impl, target)))
					return;

				methodDef.Overrides.Add(new MethodOverride(methodDef, target));
			}
			else if (target is IMemberDef targetDef) {
				// Reaching this place means that a slot of the base type is overwritten by a specific interface.
				// In case the this type is implementing the interface responsible for this, we need to declare
				// this as an override reference. If the base type is implementing the interface (as well), this
				// declaration is redundant.
				var overrideRefRequired = true;
				if (targetDef.DeclaringType.IsInterface) {
					var baseTypeDef = thisType.BaseType?.ResolveTypeDef();
					if (!(baseTypeDef is null)) {
						var baseTypeVTable = service.GetVTables()[baseTypeDef];
						if (baseTypeVTable.InterfaceSlots.TryGetValue(targetDef.DeclaringType.ToTypeSig(), out var ifcSlots)) {
							overrideRefRequired = !ifcSlots.Contains(slot);
						}
					}
				}
				if (overrideRefRequired)
					CreateOverrideReference(service, methodDef, targetDef);
			}
		}

		private static bool IsMatchingOverride(MethodOverride methodOverride, IMethodDefOrRef targetMethod) {
			SigComparer comparer = default;

			var targetDeclTypeDef = targetMethod.DeclaringType.ResolveTypeDef();
			var overrideDeclTypeDef = methodOverride.MethodDeclaration.DeclaringType.ResolveTypeDef();
			if (!comparer.Equals(targetDeclTypeDef, overrideDeclTypeDef))
				return false;

			var targetMethodSig = targetMethod.MethodSig;
			var overrideMethodSig = methodOverride.MethodDeclaration.MethodSig;

			targetMethodSig = ResolveGenericSignature(targetMethod, targetMethodSig);
			overrideMethodSig = ResolveGenericSignature(methodOverride.MethodDeclaration, overrideMethodSig);

			return comparer.Equals(targetMethodSig, overrideMethodSig);
		}

		static MethodSig ResolveGenericSignature(IMemberRef method, MethodSig overrideMethodSig) {
			if (method.DeclaringType is TypeSpec spec && spec.TypeSig is GenericInstSig genericInstSig) {
				overrideMethodSig = GenericArgumentResolver.Resolve(overrideMethodSig, genericInstSig.GenericArguments);
			}

			return overrideMethodSig;
		}

		public void PreRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			//
		}

		public void PostRename(ConfuserContext context, INameService service, ProtectionParameters parameters, IDnlibDef def) {
			var method = def as MethodDef;
			if (method == null || !method.IsVirtual || method.Overrides.Count == 0)
				return;

			method.Overrides
				  .RemoveWhere(impl => MethodEqualityComparer.CompareDeclaringTypes.Equals(impl.MethodDeclaration, method));
		}
	}
}
