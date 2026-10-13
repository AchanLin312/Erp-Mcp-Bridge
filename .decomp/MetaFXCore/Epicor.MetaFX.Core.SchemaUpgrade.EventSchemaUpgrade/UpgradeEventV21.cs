using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV21 : UpgradeTarget
{
	private class Root
	{
		[JsonProperty(PropertyName = "$schema")]
		public string schema { get; set; }

		public string contentVersion { get; set; }

		public JArray imports { get; set; }

		public JArray events { get; set; }
	}

	private List<string> onEvents = new List<string> { "onSuccess", "onFailure", "onNoMatch", "onSingleMatch", "onMultipleMatch", "onError", "onYes", "onNo" };

	public ILogger _logger { get; set; }

	public UpgradeEventV21(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.1/events.json";
		root.contentVersion = "2.1";
		root.imports = schema["imports"];
		root.events = UpgradeEvents(schema["events"]);
		Root root2 = root;
		return Deserialize(root2);
	}

	private JArray UpgradeEvents(JArray events)
	{
		foreach (JToken @event in events)
		{
			if (@event.Type != JTokenType.Comment)
			{
				JToken action = @event["action"];
				onEvents.ForEach(delegate(string e)
				{
					RemoveOnEventActions(action, e);
				});
			}
		}
		ConvertEventActionToActionsArray(events);
		return events;
	}

	private void RemoveOnEventActions(JToken action, string onEventName)
	{
		if (action == null)
		{
			return;
		}
		JToken onEventAction = GetOnEventAction(action, onEventName);
		if (onEventAction != null)
		{
			JToken jToken = onEventAction["actions"];
			if (jToken != null)
			{
				ConvertOnEventActions(jToken);
				onEventAction.Replace(jToken);
			}
		}
	}

	private JToken GetOnEventAction(JToken action, string onEventName)
	{
		if (action[onEventName] != null)
		{
			return action[onEventName];
		}
		if (action["param"] is JObject)
		{
			return action["param"][onEventName];
		}
		return null;
	}

	private void ConvertOnEventActions(JToken actions)
	{
		foreach (JToken action in (IEnumerable<JToken>)actions)
		{
			if (action == null || action.Type == JTokenType.Comment)
			{
				continue;
			}
			onEvents.ForEach(delegate(string e)
			{
				JToken onEventAction = GetOnEventAction(action, e);
				if (onEventAction != null)
				{
					JToken jToken = onEventAction["actions"];
					if (jToken != null)
					{
						ConvertOnEventActions(jToken);
					}
					onEventAction.Replace(jToken);
				}
			});
		}
	}

	private void ConvertEventActionToActionsArray(JArray events)
	{
		foreach (JToken @event in events)
		{
			if (@event.Type != JTokenType.Comment)
			{
				JToken jToken = @event["action"];
				if (jToken != null && jToken.Type != JTokenType.Comment)
				{
					JArray jArray = new JArray();
					jArray.Add(jToken);
					(@event as JObject).Add("actions", jArray);
					(@event as JObject).Property("action").Remove();
				}
			}
		}
	}
}
