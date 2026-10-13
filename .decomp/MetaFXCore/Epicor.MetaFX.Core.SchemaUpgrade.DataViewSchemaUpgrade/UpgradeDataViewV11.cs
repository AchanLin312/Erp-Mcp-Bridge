using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.DataViewSchemaUpgrade;

internal class UpgradeDataViewV11 : UpgradeTarget
{
	public UpgradeDataViewV11(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema["$schema"] = "https://schemas.developer.epicor.com/schemas/v1.1/dataviews.json";
		schema["contentVersion"] = "1.1";
		return Deserialize(schema);
	}
}
