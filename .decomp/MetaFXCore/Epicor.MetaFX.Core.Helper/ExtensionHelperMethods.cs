using System;

namespace Epicor.MetaFX.Core.Helper;

internal static class ExtensionHelperMethods
{
	public static bool IsEqual(this string one, string two)
	{
		return string.Equals(one?.Trim(), two?.Trim(), StringComparison.OrdinalIgnoreCase);
	}
}
