using System;
using System.Globalization;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

internal class PCConvert
{
	public static decimal ParseDecimal(string value)
	{
		return decimal.Parse(value, CultureInfo.InvariantCulture);
	}

	public static int ParseInt32(string value)
	{
		return int.Parse(value, CultureInfo.InvariantCulture);
	}

	public static bool TryParse(string value, out decimal result)
	{
		bool result2 = true;
		try
		{
			result = ParseDecimal(value);
		}
		catch
		{
			result = 0m;
			result2 = false;
		}
		return result2;
	}

	public static bool TryParse(string value, out int result)
	{
		bool result2 = true;
		try
		{
			result = ParseInt32(value);
		}
		catch
		{
			result = 0;
			result2 = false;
		}
		return result2;
	}

	public static bool ToBoolean(string value)
	{
		if ("no".StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!"yes".StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			return bool.Parse(value);
		}
		return true;
	}
}
