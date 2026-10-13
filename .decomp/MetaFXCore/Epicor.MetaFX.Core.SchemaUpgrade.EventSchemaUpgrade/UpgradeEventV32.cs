using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV32 : UpgradeTarget
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

	public UpgradeEventV32(ILogger logger)
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
		root.schema = "https://schemas.developer.epicor.com/schemas/v3.2/events.json";
		root.contentVersion = "3.2";
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
			if (!((item.Type == JTokenType.Comment) ? true : false))
			{
				if (item.type == "rest-erp")
				{
					Convert(item);
					RecursiveAction(item);
				}
				else
				{
					RecursiveAction(item);
				}
			}
		}
	}

	private void RecursiveAction(dynamic action)
	{
		if (action.param != null && action.param is JObject && action.param["onFailure"] != null)
		{
			if (!((action.param.onFailure is JObject && action.param.onFailure.actions != null) ? true : false))
			{
				ExecuteAction(action.param["onFailure"]);
			}
			else
			{
				ExecuteAction(action.param["onFailure"]["actions"]);
			}
		}
		if (action.param != null && action.param is JObject && action.param["onOk"] != null)
		{
			if (!((action.param.onOk is JObject && action.param.onOk.actions != null) ? true : false))
			{
				ExecuteAction(action.param["onOk"]);
			}
			else
			{
				ExecuteAction(action.param["onOk"]["actions"]);
			}
		}
		if (action.param != null && action.param is JObject && action.param["onNo"] != null)
		{
			ExecuteAction(action.param["onNo"]);
		}
		if (action.param != null && action.param is JObject && action.param["cases"] != null)
		{
			ExecuteAction(action.param["cases"]);
		}
		if (action.actions != null)
		{
			ExecuteAction(action.actions);
		}
		if (action.onNo != null)
		{
			ExecuteAction(action.onNo);
		}
		if (action.param != null && action.param is JObject && action.param["onSuccess"] != null)
		{
			if (action.param.onSuccess is JObject && action.param.onSuccess.actions != null)
			{
				ExecuteAction(action.param.onSuccess.actions);
			}
			else
			{
				ExecuteAction(action.param["onSuccess"]);
			}
		}
		else if (action.onYes != null)
		{
			ExecuteAction(action.onYes);
		}
		else if (action.onOk != null)
		{
			if (action.onOk is JObject && action.onOk.actions != null)
			{
				ExecuteAction(action.onOk.actions);
			}
			else
			{
				ExecuteAction(action.onOk);
			}
		}
		else if (action.onSuccess != null)
		{
			if (action.onSuccess is JObject && action.onSuccess.actions != null)
			{
				ExecuteAction(action.onSuccess.actions);
			}
			else
			{
				ExecuteAction(action.onSuccess);
			}
		}
		else if (action.onFailure != null)
		{
			ExecuteAction(action.onFailure);
		}
	}

	private void ExecuteAction(dynamic actions)
	{
		foreach (dynamic action in actions)
		{
			if (action.Type == JTokenType.Comment)
			{
				continue;
			}
			try
			{
				if (!((action.type != null && action.type == "rest-erp") ? true : false))
				{
					RecursiveAction(action);
					continue;
				}
				Convert(action);
				RecursiveAction(action);
			}
			catch
			{
				logger.Debug("Dirty row not found");
			}
		}
	}

	private void Convert(dynamic action)
	{
		dynamic val = action["param"];
		if (val["erpRestPostArgs"] != null && val["erpRestPostArgs"]["requestParamArgs"] != null)
		{
			Move(val, "requestParamArgs");
		}
		else if (val["erpRestPostArgs"] != null && val["erpRestPostArgs"]["responseParamArgs"] != null)
		{
			Move(val, "responseParamArgs");
		}
	}

	private void Move(dynamic param, string args)
	{
		dynamic val = param["erpRestPostArgs"][args];
		JArray jArray = new JArray();
		foreach (dynamic item in val)
		{
			string empty = string.Empty;
			if (!((item.Type == JTokenType.Comment) ? true : false) && ((item["dirtyRow"] != null) ? true : false))
			{
				dynamic val2 = item["dirtyRow"].Value.Split(':');
				empty = ((val2.Length == 2) ? val2[1] : val2[0]);
				JProperty content = new JProperty("view", empty);
				jArray.Add(new JObject(content));
				(item as JObject).Remove("dirtyRow");
			}
		}
		if (jArray.Count > 0)
		{
			(param as JObject).Add("dirtyRows", JArray.FromObject(jArray));
		}
	}
}
