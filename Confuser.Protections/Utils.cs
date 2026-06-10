using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Confuser.Core;
using dnlib.DotNet;

namespace Confuser.Protections {
	internal static class Utils {
		public static IMethod Import(this ModuleDef module, ConfuserContext context, Type classType, string method) {
			var corLib = context.Resolver.Resolve(context.CurrentModule?.CorLibTypes.AssemblyRef, context.CurrentModule);
			var typeInfo = corLib?.ManifestModule.Find(classType.FullName, true);
			return (typeInfo == null) ? module.Import(classType.GetMethod(method)) : module.Import(typeInfo.FindMethod(method));
		}
	}
}
