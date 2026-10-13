using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV11 : UpgradeTarget
{
	public UpgradeEventV11(ILogger logger)
		: base(logger)
	{
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.1/events.json";
		root.contentVersion = "1.1";
		root.imports = schema["imports"];
		root.events = UpgradeEvents(schema["events"]);
		root.patch = schema["patch"];
		Root root2 = root;
		return Deserialize(root2);
	}

	private object UpgradeEvents(dynamic oldSchema)
	{
		List<EventV11> list = new List<EventV11>();
		foreach (dynamic item in oldSchema)
		{
			EventV11 eventV = new EventV11();
			eventV.id = item.id;
			eventV.description = item.description;
			eventV.trigger = (object)item.trigger;
			EventV11 eventV2 = eventV;
			if (item.action != null)
			{
				eventV2.action = new ActionV11();
				eventV2.action.type = item.action.type;
				eventV2.action.param = (object)item.action.param;
				if (item.onSuccess != null)
				{
					eventV2.action.onSuccess = (object)item.onSuccess;
				}
				if (item.onError != null)
				{
					eventV2.action.onError = (object)item.onError;
				}
				eventV2.action.value = item.action.value;
				eventV2.action.option = (object)item.action.option;
				eventV2.action.epBinding = item.action.epBinding;
				eventV2.action.dataview = item.action.dataview;
			}
			list.Add(eventV2);
		}
		return list;
	}
}
