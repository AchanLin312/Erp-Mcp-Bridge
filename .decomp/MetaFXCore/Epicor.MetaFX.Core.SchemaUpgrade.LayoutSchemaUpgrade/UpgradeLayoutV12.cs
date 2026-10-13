using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV12 : UpgradeTarget
{
	public UpgradeLayoutV12(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		FlattenLayout flattenLayout = new FlattenLayout();
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.2/layout.json";
		root.contentVersion = "1.2";
		root.imports = schema["imports"];
		root.layout = (object)flattenLayout.ConvertToFlattenLayout(schema["layout"]);
		root.patch = (object)schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}
}
