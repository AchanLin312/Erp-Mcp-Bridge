using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epicor.MetaFX.Core.Models.MetaUI.RowRules;

public class RulesSchema : BaseSchema
{
	[JsonProperty(PropertyName = "rules", Order = 10)]
	public List<RowRuleBase> RowRules { get; set; }

	public RulesSchema()
	{
		RowRules = new List<RowRuleBase>();
	}
}
