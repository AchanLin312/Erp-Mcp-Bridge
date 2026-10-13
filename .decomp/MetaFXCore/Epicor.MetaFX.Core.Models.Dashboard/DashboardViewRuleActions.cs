using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
public class DashboardViewRuleActions
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string RuleID { get; set; }

	public string ColumnName { get; set; }

	public string SettingStyle { get; set; }

	public string ForeignKeyName { get; set; }

	public string IsImageSetting { get; set; }
}
