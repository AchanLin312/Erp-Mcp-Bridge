using System;
using System.Collections.Generic;
using System.Linq;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.RowRules;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

internal class RowRulesManager
{
	private const string DASHBOARD_ROWRULE_CONDITION_EQUAL = "equals";

	private const string DASHBOARD_ROWRULE_CONDITION_NOTEQUAL = "notequal";

	private const string DASHBOARD_ROWRULE_CONDITION_GREATERTHAN = "greaterthan";

	private const string DASHBOARD_ROWRULE_CONDITION_LESSTHAN = "lessthan";

	private const string DASHBOARD_ROWRULE_CONDITION_GREATERTHANOREQUALTO = "greaterthanorequalto";

	private const string DASHBOARD_ROWRULE_CONDITION_LESSTHANOREQUALTO = "lessthanorequalto";

	private const string DASHBOARD_ROWRULE_CONDITION_STARTSWITH = "startswith";

	private const string DASHBOARD_ROWRULE_CONDITION_ENDSWITH = "endswith";

	private const string DASHBOARD_ROWRULE_CONDITION_CONTAINS = "contains";

	private const string DASHBOARD_ROWRULE_AddRowSettings = "AddRowSettings";

	private const string DateFormat = "yyyy-MM-dd";

	private readonly DashboardArgs dashboardArgs;

	private readonly DashboardDataSet dashboardDataSet;

	public RowRulesManager(DashboardArgs dashboardArgs)
	{
		this.dashboardArgs = dashboardArgs;
		dashboardDataSet = dashboardArgs.DashboardDataSet;
	}

	public RulesSchema GenerateSchema()
	{
		RulesSchema rulesSchema = new RulesSchema();
		rulesSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1/rules.json";
		rulesSchema.ContentVersion = "1.0";
		rulesSchema.Imports = new object[1]
		{
			new
			{
				rules = new Reference
				{
					Refs = dashboardArgs.ImportPath + "/rules.jsonc#/rules"
				}
			}
		};
		return rulesSchema;
	}

	public List<RowRuleBase> GenerateRules(DashboardViewArgs dashboardViewArgs)
	{
		DashboardQuery dashboardQuery = dashboardViewArgs.DashboardQuery;
		DashboardView dashboardView = dashboardViewArgs.DashboardView;
		List<RowRuleBase> list = new List<RowRuleBase>();
		foreach (DashboardViewRules item in dashboardDataSet.DashboardViewRules.Where((DashboardViewRules dbd) => dbd.QueryID == dashboardQuery.QueryID && dbd.ViewID == dashboardView.ViewID))
		{
			RowRuleBase rule = GetRule(dashboardViewArgs.DataViewName, dashboardViewArgs, item);
			list.Add(rule);
			if (dashboardViewArgs.IsTrackerEmbedGrid)
			{
				RowRuleBase rule2 = GetRule(dashboardArgs.DashboardHelper.GetDataviewNameForEmbedGrid(dashboardViewArgs), dashboardViewArgs, item);
				list.Add(rule2);
			}
		}
		list.Reverse();
		return list;
	}

	private RowRuleBase GetRule(string dataViewName, DashboardViewArgs dashboardViewArgs, DashboardViewRules dashboardViewRule)
	{
		RowRuleBase rowRuleBase = new RowRuleBase();
		rowRuleBase.Name = Guid.NewGuid().ToString();
		rowRuleBase.DataView = dataViewName;
		if (dashboardDataSet.DashboardViewColumns.Any((DashboardViewColumns x) => x.ColumnName == dashboardViewRule.Argument2))
		{
			rowRuleBase.Condition = "[" + dataViewName + "].[" + dashboardViewRule.Argument1 + "]  " + GetConditionAsText(dashboardViewRule.Condition) + "  [" + dataViewName + "].[" + dashboardViewRule.Argument2 + "] ";
		}
		else
		{
			rowRuleBase.Condition = dataViewName + "." + dashboardViewRule.Argument1 + "  " + GetConditionAsText(dashboardViewRule.Condition) + "  " + GetValue(dashboardArgs.DashboardDataSet, dashboardViewArgs.DashboardView, dashboardViewRule.Argument1, dashboardViewRule.Argument2, dashboardViewRule.Condition);
		}
		if (rowRuleBase.Condition.Contains("Constant:"))
		{
			rowRuleBase.Condition = rowRuleBase.Condition.Replace("Constant:", "Constant.")?.Replace("'", "")?.Replace(" ", "");
		}
		IEnumerable<DashboardViewRuleActions> enumerable = dashboardDataSet.DashboardViewRuleActions?.Where((DashboardViewRuleActions dbd) => dbd.QueryID == dashboardViewArgs.DashboardQuery.QueryID && dbd.ViewID == dashboardViewArgs.DashboardView.ViewID && dbd.RuleID == dashboardViewRule.RuleID);
		List<object> list = new List<object>();
		if (enumerable != null)
		{
			foreach (DashboardViewRuleActions item in enumerable)
			{
				object action = GetAction(item, dataViewName);
				list.Add(action);
			}
			rowRuleBase.Actions = list.ToArray();
		}
		return rowRuleBase;
	}

	private string GetValue(DashboardDataSet dashboardDataSet, DashboardView dashboardView, string columnName, string val, string condition)
	{
		string text = dashboardDataSet.DashboardViewColumns?.FirstOrDefault((DashboardViewColumns c) => c.QueryID == dashboardView.QueryID && c.ViewID == dashboardView.ViewID && c.ColumnName == columnName)?.DataType;
		if (text == "date")
		{
			if (val.Contains("Constant:"))
			{
				val = val.Replace("Constant:", "Constant.")?.Replace("'", "")?.Replace(" ", "");
			}
			else
			{
				DateTime.Parse(val);
				val = DateTime.Parse(val).ToString("yyyy-MM-dd");
			}
		}
		return condition.ToLowerInvariant() switch
		{
			"startswith" => dashboardArgs.DashboardHelper.GetValue(null, val + "%"), 
			"contains" => dashboardArgs.DashboardHelper.GetValue(null, "%" + val + "%"), 
			"endswith" => dashboardArgs.DashboardHelper.GetValue(null, "%" + val), 
			_ => dashboardArgs.DashboardHelper.GetValue(text, val), 
		};
	}

	public void CreateFile(RulesSchema ruleSchema)
	{
		JObject data = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(ruleSchema), "rules");
		dashboardArgs.HelperMethods.CreateJsonFile(data, dashboardArgs.OutputPath, "rules.jsonc");
	}

	public string GetConditionAsText(string dashboardCondition)
	{
		string result = string.Empty;
		switch (dashboardCondition.ToLowerInvariant())
		{
		case "equals":
			result = "=";
			break;
		case "notequal":
			result = "<>";
			break;
		case "greaterthan":
			result = ">";
			break;
		case "greaterthanorequalto":
			result = ">=";
			break;
		case "lessthan":
			result = "<";
			break;
		case "lessthanorequalto":
			result = "<=";
			break;
		case "contains":
		case "endswith":
		case "startswith":
			result = "like";
			break;
		}
		return result;
	}

	private object GetAction(DashboardViewRuleActions dashboardViewRuleActions, string dataViewName)
	{
		bool flag = false;
		string empty = string.Empty;
		if (dashboardViewRuleActions.ColumnName == "AddRowSettings")
		{
			flag = true;
		}
		empty = ((dashboardViewRuleActions.SettingStyle.ToLowerInvariant() == "ok") ? ((!flag) ? "Ok" : "RowOk") : ((!flag) ? dashboardViewRuleActions.SettingStyle : ("Row" + dashboardViewRuleActions.SettingStyle)));
		if (flag)
		{
			return new
			{
				action = "SettingStyle." + empty
			};
		}
		if (dashboardViewRuleActions.IsImageSetting == "true")
		{
			return new
			{
				action = "ChangeImage",
				value = empty,
				epBinding = dataViewName + "." + dashboardViewRuleActions.ColumnName
			};
		}
		return new
		{
			action = "SettingStyle." + empty,
			epBinding = dataViewName + "." + dashboardViewRuleActions.ColumnName
		};
	}
}
