using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.DataViewSchemaUpgrade;

internal class UpgradeDataViewV40 : UpgradeTarget
{
	public UpgradeDataViewV40(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		schema["$schema"] = "https://schemas.developer.epicor.com/schemas/v4.0/dataviews.json";
		schema["contentVersion"] = "4.0";
		return Deserialize(schema);
	}
}
