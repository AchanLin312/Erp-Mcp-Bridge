using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.RowRuleSchemaUpgrade;

internal class UpgradeRowRuleV30 : UpgradeTarget
{
	public UpgradeRowRuleV30(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema["$schema"] = "https://schemas.developer.epicor.com/schemas/v3.0/rules.json";
		schema["contentVersion"] = "3.0";
		return Deserialize(schema);
	}
}
