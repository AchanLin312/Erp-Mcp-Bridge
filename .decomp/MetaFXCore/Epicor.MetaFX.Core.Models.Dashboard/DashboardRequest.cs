using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Dashboard;

public class DashboardRequest
{
	public DashboardDefinition DashboardDefinition { get; set; }

	public DashboardDataSet DashboardDataSet { get; set; }

	public List<DynamicQueryDefinition> DynamicQueryDefinitions { get; set; }
}
