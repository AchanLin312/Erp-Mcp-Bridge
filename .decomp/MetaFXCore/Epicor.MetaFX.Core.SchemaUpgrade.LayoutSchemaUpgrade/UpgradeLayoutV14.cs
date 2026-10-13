using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.LayoutSchemaUpgrade;

internal class UpgradeLayoutV14 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JObject layout { get; set; }

		public dynamic patch { get; set; }
	}

	public UpgradeLayoutV14(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		new FlattenLayout();
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.4/layout.json";
		root.contentVersion = "1.4";
		root.imports = null;
		root.layout = ((schema["layout"] != null) ? Convert(schema["layout"]) : null);
		root.patch = (object)schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private JObject Convert(JObject oldLayout)
	{
		AddNavigation(oldLayout);
		return oldLayout;
	}

	private void AddNavigation(JObject oldLayout)
	{
		JObject jObject = new JObject();
		jObject.Add(new JProperty("pageCaption", string.Empty));
		jObject.Add(new JProperty("tabId", string.Empty));
		jObject.Add(new JProperty("enable", false));
		jObject.Add(new JProperty("parentPage", string.Empty));
		jObject.Add(new JProperty("epBinding", string.Empty));
		jObject.Add(new JProperty("epBindingLevel", 0));
		jObject.Add(new JProperty("epBindingPeerOrder", 0));
		jObject["parentPage"] = oldLayout["parentPage"];
		jObject["epBinding"] = oldLayout["epBinding"];
		oldLayout.Remove("parentPage");
		oldLayout.Remove("epBinding");
		oldLayout.Remove("primaryDataView");
		oldLayout.Add("navigation", jObject);
	}
}
