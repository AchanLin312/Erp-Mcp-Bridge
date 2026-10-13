using System;
using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Dashboard;

internal class PanelCardGridArgs
{
	public string ComponentId { get; private set; }

	public List<DashboardViewArgs> DashboardViewArgsList { get; set; }

	public DashboardQuery DashboardQuery { get; set; }

	public List<DashboardViewArgs> SliderDashboardViews { get; set; }

	public List<BAQWhereExpression> ServerFilter { get; set; }

	public List<List<BAQWhereExpression>> SearchFilters { get; set; }

	public List<DashboardViewColumns> DashboardViewColumns { get; set; }

	public List<DashboardViewImageColumns> DashboardViewImageColumns { get; set; }

	public DynamicQueryDefinition DynamicQueryDefinition { get; set; }

	internal PanelCardGridArgs()
	{
		DashboardViewArgsList = new List<DashboardViewArgs>();
		DashboardViewColumns = new List<DashboardViewColumns>();
		SearchFilters = new List<List<BAQWhereExpression>>();
		SliderDashboardViews = new List<DashboardViewArgs>();
		ServerFilter = new List<BAQWhereExpression>();
		ComponentId = Guid.NewGuid().ToString();
	}
}
