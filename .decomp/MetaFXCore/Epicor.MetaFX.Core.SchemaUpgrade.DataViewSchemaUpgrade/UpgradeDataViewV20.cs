using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.DataViewSchemaUpgrade;

internal class UpgradeDataViewV20 : UpgradeTarget
{
	public UpgradeDataViewV20(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema["$schema"] = "https://schemas.developer.epicor.com/schemas/v2.0/dataviews.json";
		schema["contentVersion"] = "2.0";
		return Deserialize(schema);
	}
}
