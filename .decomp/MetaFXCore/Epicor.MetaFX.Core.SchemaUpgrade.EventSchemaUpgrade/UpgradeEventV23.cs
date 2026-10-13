using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

internal class UpgradeEventV23 : UpgradeTarget
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

	private string value = "";

	public ILogger _logger { get; set; }

	public UpgradeEventV23(ILogger logger)
		: base(logger)
	{
		_logger = logger;
		AddEventType();
	}

	public override JObject Upgrade(JObject json)
	{
		return UpgradeSchema(json);
	}

	private JObject UpgradeSchema(dynamic schema)
	{
		Root root = new Root();
		root.schema = "https://schemas.developer.epicor.com/schemas/v2.3/events.json";
		root.contentVersion = "2.3";
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
				JToken jToken = @event["actions"];
				ReplaceActiontype(jToken as JArray);
			}
		}
		return events;
	}

	private void ReplaceActiontype(JToken actions)
	{
		if (actions == null)
		{
			return;
		}
		if (actions is JObject)
		{
			foreach (JProperty item in (actions as JObject).Properties())
			{
				if (item.Name == "type" && (string?)item.Value != null && EventType.TryGetValue((string?)item.Value, out value))
				{
					EventType.TryGetValue((string?)item.Value, out value);
					if (!(value == "") && value != null)
					{
						item.Value = value;
					}
				}
				ReplaceActiontype(item.Value);
			}
			return;
		}
		if (!(actions is JArray))
		{
			return;
		}
		foreach (JToken item2 in (IEnumerable<JToken>)actions)
		{
			if (item2 is JObject)
			{
				if ((string?)item2["type"] != null)
				{
					EventType.TryGetValue((string?)item2["type"], out value);
				}
				if (!(value == "") && value != null)
				{
					item2["type"] = value;
				}
				ReplaceActiontype(item2);
			}
		}
	}

	private void AddEventType()
	{
		EventType.Add("nextEvent", "event-next");
		EventType.Add("close-view", "app-close");
		EventType.Add("openView", "app-open");
		EventType.Add("setFocus", "control-focus-set");
		EventType.Add("clearTimer", "dashboard-timer-clear");
		EventType.Add("setTimer", "dashboard-timer-set");
		EventType.Add("commit", "data-commit");
		EventType.Add("rollback", "data-rollback");
		EventType.Add("clearDataView", "dataview-clear");
		EventType.Add("viewCondition", "dataview-condition");
		EventType.Add("copyView", "dataview-copy");
		EventType.Add("setFilter", "dataview-filter-set");
		EventType.Add("showDialog", "dialog-show");
		EventType.Add("showSystemDialog", "dialog-show-system");
		EventType.Add("cancelEvent", "event-cancel");
		EventType.Add("disableColumnChangeEvents", "event-column-disable");
		EventType.Add("enableColumnChangeEvents", "event-column-enable");
		EventType.Add("completeEvent", "event-complete");
		EventType.Add("disableRowChangeEvents", "event-row-disable");
		EventType.Add("enableRowChangeEvents", "event-row-enable");
		EventType.Add("disableViewChangeEvents", "event-view-disable");
		EventType.Add("enableViewChangeEvents", "event-view-enable");
		EventType.Add("externalComponent", "external-component");
		EventType.Add("erpFileTransfer", "file-transfer-erp");
		EventType.Add("set-variable", "global-variable-set");
		EventType.Add("navigatePage", "page-navigate-to");
		EventType.Add("setImage", "image-set");
		EventType.Add("updateRow", "row-update");
		EventType.Add("showSearch", "search-show");
		EventType.Add("setSearchValue", "search-value-set");
		EventType.Add("close-slider", "slider-close");
		EventType.Add("open-slider", "slider-open");
		EventType.Add("setTitle", "title-set");
		EventType.Add("launchURL", "url-open");
		EventType.Add("setProperty", "property-set");
		EventType.Add("setCurrentRow", "row-current-set");
		EventType.Add("proccessImportService", "process-import-service");
		EventType.Add("deleteRow", "row-delete");
		EventType.Add("copyRow", "row-copy");
		EventType.Add("erpRest", "rest-erp");
		EventType.Add("clear-data", "data-clear");
		EventType.Add("comboRefill", "combo-refill");
		EventType.Add("setValue", "value-set");
	}
}
