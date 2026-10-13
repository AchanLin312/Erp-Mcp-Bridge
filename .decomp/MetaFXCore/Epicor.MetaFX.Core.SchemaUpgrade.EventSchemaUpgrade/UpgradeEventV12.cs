using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV12 : UpgradeTarget
{
	public ILogger _logger { get; set; }

	public UpgradeEventV12(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v1.2/events.json";
		root.contentVersion = "1.2";
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
			if (item.action != null && item.action.onSuccess != null && item.action.onSuccess.actions != null)
			{
				foreach (dynamic item2 in item.action.onSuccess.actions)
				{
					if (item2.condition != null)
					{
						dynamic val = item2.type;
						dynamic val2 = item2.value;
						dynamic val3 = item2.condition;
						item2.type = "condition";
						dynamic val4 = new Param
						{
							expression = val3,
							onSuccess = new OnSuccess
							{
								actions = new List<SuccessAction>
								{
									new SuccessAction
									{
										type = val,
										value = val2
									}
								}
							}
						};
						dynamic val5 = JsonConvert.SerializeObject(val4, new JsonSerializerSettings
						{
							Formatting = Formatting.None,
							NullValueHandling = NullValueHandling.Ignore
						});
						(item2 as JObject).Add("param", JToken.Parse(val5));
						(item2 as JObject).Remove("value");
						(item2 as JObject).Remove("condition");
					}
				}
			}
			dynamic val6 = item;
			EventV11 eventV = new EventV11();
			eventV.id = val6.id;
			eventV.description = val6.description;
			eventV.trigger = (object)val6.trigger;
			EventV11 eventV2 = eventV;
			if (val6.action != null)
			{
				eventV2.action = new ActionV11();
				eventV2.action.type = val6.action.type;
				eventV2.action.param = (object)val6.action.param;
				eventV2.action.onSuccess = (object)val6.action.onSuccess;
				eventV2.action.onError = (object)val6.action.onError;
				eventV2.action.value = val6.action.value;
				eventV2.action.option = (object)val6.action.option;
				eventV2.action.epBinding = val6.action.epBinding;
				eventV2.action.dataview = val6.action.dataview;
			}
			list.Add(eventV2);
		}
		return list;
	}
}
