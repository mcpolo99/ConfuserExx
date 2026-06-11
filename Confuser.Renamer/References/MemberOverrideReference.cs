using System;
using System.Diagnostics;
using System.Text;
using Confuser.Core;
using dnlib.DotNet;

namespace Confuser.Renamer.References {
	public sealed class MemberOverrideReference : INameReference<IDnlibDef> {
		internal IMemberDef ThisMemberDef { get; }
		internal IMemberDef BaseMemberDef { get; }

		public bool ShouldCancelRename => ThisMemberDef.Module != BaseMemberDef.Module;

		public MemberOverrideReference(IMemberDef thisMemberDef, IMemberDef baseMemberDef) {
			ThisMemberDef = thisMemberDef ?? throw new ArgumentNullException(nameof(thisMemberDef));
			BaseMemberDef = baseMemberDef ?? throw new ArgumentNullException(nameof(baseMemberDef));
			Debug.Assert(thisMemberDef != baseMemberDef);
		}

		/// <inheritdoc />
		public bool DelayRenaming(INameService service, IDnlibDef currentDef) =>
			currentDef != BaseMemberDef
			&& !ShouldCancelRename
			&& !service.IsRenamed(BaseMemberDef);

		public bool UpdateNameReference(ConfuserContext context, INameService service) {
			if (UTF8String.Equals(ThisMemberDef.Name, BaseMemberDef.Name)) return false;
			ThisMemberDef.Name = BaseMemberDef.Name;
			return true;
		}

		public override string ToString() => ToString(null);

		public string ToString(INameService nameService) {
			var builder = new StringBuilder();
			builder.Append("Member Override Reference").Append("(");
			builder.Append("This ").AppendReferencedDef(ThisMemberDef, nameService);
			builder.Append("; ");
			builder.Append("Base ").AppendReferencedDef(BaseMemberDef, nameService);
			builder.Append(")");
			return builder.ToString();
		}
	}
}
