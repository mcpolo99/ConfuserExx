using System;
using Confuser.Core.Services;

namespace Confuser.Protections.AntiTamper {
	/// <summary>
	///     Picks the rotation amounts for the anti-tamper key-derivation mixer, replacing the fixed
	///     5 / 3 / 7 / 11 that de4dot/AV pattern-match. A bit rotation is a bijection for any amount
	///     in 1..31, so -- unlike the xorshift/prime constants -- no curated set is required; the
	///     four amounts are simply chosen distinct per module to keep the 16-word key expansion
	///     spread. The same amounts are injected into the runtime (Mutation.KeyI6..KeyI9), which
	///     forms each rotation as (r >> amount) | (r << (32 - amount)).
	/// </summary>
	internal static class RotationKey {
		internal static int[] PickShifts(RandomGenerator random) {
			var shifts = new int[4];
			for (int i = 0; i < shifts.Length; i++) {
				int s;
				do {
					s = random.NextInt32(1, 32);
				} while (Array.IndexOf(shifts, s, 0, i) != -1);
				shifts[i] = s;
			}
			return shifts;
		}
	}
}
