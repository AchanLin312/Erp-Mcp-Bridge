using System;

namespace Epicor.MetaFX.Core.Models.Dashboard;

[Serializable]
public class DashboardViewRules
{
	public string Company { get; set; }

	public string DefinitionID { get; set; }

	public string QueryID { get; set; }

	public string ViewID { get; set; }

	public string RuleID { get; set; }

	public string Argument1 { get; set; }

	public string Argument2 { get; set; }

	public string Condition { get; set; }

	public string ForeignKeyName { get; set; }

	public string Arg1Type { get; set; }

	public string Arg2Type { get; set; }
}
