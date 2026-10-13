using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.DataViewSchemaUpgrade;

internal class UpgradeDataViewV30 : UpgradeTarget
{
	public UpgradeDataViewV30(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema["$schema"] = "https://schemas.developer.epicor.com/schemas/v3.0/dataviews.json";
		schema["contentVersion"] = "3.0";
		return Deserialize(schema);
	}
}
