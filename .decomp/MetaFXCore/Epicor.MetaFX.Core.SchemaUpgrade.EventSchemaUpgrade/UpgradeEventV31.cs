using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV31 : UpgradeTarget
{
	public class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray events { get; set; }
	}

	private Dictionary<string, string> EventType = new Dictionary<string, string>();

	public ILogger _logger { get; set; }

	public UpgradeEventV31(ILogger logger)
		: base(logger)
	{
		_logger = logger;
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v3.1/events.json";
		root.contentVersion = "3.1";
		root.imports = schema["imports"];
		root.events = UpgradeEvents(schema["events"]);
		Root root2 = root;
		return Deserialize(root2);
	}

	private JArray UpgradeEvents(JArray events)
	{
		ConvertDirtyRow(events);
		return events;
	}

	private void ConvertDirtyRow(JArray events)
	{
		foreach (JToken @event in events)
		{
			CheckAndUpgradeEvent(@event as JObject);
		}
	}

	private void CheckAndUpgradeEvent(dynamic component)
	{
		if (component == null)
		{
			return;
		}
		dynamic val = component.actions;
		if (!((val != null) ? true : false))
		{
			return;
		}
		foreach (dynamic item in val)
		{
			if ((item.Type == JTokenType.Comment) || !((item.type == "rest-erp") ? true : false))
			{
				continue;
			}
			dynamic val2 = item["param"];
			if (!((val2["erpRestPostArgs"] != null && val2["erpRestPostArgs"]["requestParamArgs"] != null) ? true : false))
			{
				continue;
			}
			dynamic val3 = val2["erpRestPostArgs"]["requestParamArgs"];
			JArray jArray = new JArray();
			foreach (dynamic item2 in val3)
			{
				if (!((item2.Type == JTokenType.Comment) ? true : false) && ((item2["dirtyRow"] != null) ? true : false))
				{
					JProperty content = new JProperty("view", item2["dirtyRow"]);
					jArray.Add(new JObject(content));
					(item2 as JObject).Remove("dirtyRow");
				}
			}
			if (jArray.Count > 0)
			{
				(val2 as JObject).Add("dirtyRows", JArray.FromObject(jArray));
			}
		}
	}
}
