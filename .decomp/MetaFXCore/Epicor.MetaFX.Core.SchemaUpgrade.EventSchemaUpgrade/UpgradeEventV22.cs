using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV22 : UpgradeTarget
{
	private class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray events { get; set; }
	}

	private readonly Dictionary<string, string> modifiedProperties = new Dictionary<string, string> { { "dataView", "dataview" } };

	private bool enableRowChangeEvents;

	private bool disableRowChangeEvents;

	public ILogger _logger { get; set; }

	public UpgradeEventV22(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.2/events.json";
		root.contentVersion = "2.2";
		root.imports = schema["imports"];
		root.events = UpgradeEvents(schema["events"]);
		Root root2 = root;
		return Deserialize(root2);
	}

	private JArray UpgradeEvents(JArray events)
	{
		ConvertEventDataView(events);
		return events;
	}

	private void ConvertEventDataView(JArray events)
	{
		foreach (JToken @event in events)
		{
			CheckAndUpdateEvent(@event as JObject);
		}
	}

	private void CheckAndUpdateEvent(JObject component)
	{
		if (component == null)
		{
			return;
		}
		foreach (JProperty item in component.Properties().ToList())
		{
			if (item.Name == "type" && item.Value.ToString() == "disableRowChangeEvents")
			{
				disableRowChangeEvents = true;
			}
			else if (item.Name == "type" && item.Value.ToString() == "enableRowChangeEvents")
			{
				enableRowChangeEvents = true;
			}
			else if (item.Name == "dataView" && (enableRowChangeEvents || disableRowChangeEvents))
			{
				component.Property(item.Name).Replace(new JProperty(modifiedProperties[item.Name], item.Value));
				enableRowChangeEvents = (disableRowChangeEvents = false);
			}
			if (item.Value.HasValues)
			{
				if (item.Value.GetType().Name == "JArray")
				{
					ConvertEventDataView(item.Value as JArray);
				}
				else if (item.Value.GetType().Name == "JObject")
				{
					CheckAndUpdateEvent(item.Value as JObject);
				}
			}
		}
	}
}
