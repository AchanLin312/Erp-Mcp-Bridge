using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Dashboard;

internal class DashboardViewArgs
{
	public DashboardQuery DashboardQuery { get; set; }

	public DashboardView DashboardView { get; set; }

	public bool IsGrid { get; set; }

	public bool IsTracker { get; set; }

	public bool IsTrackerEmbedGrid { get; set; }

	public bool HasGridGroupBy { get; set; }

	public bool IsUpdatable { get; set; }

	public bool AllowAddNew { get; set; }

	public string AddNewLabel { get; set; }

	public bool IsSearchTrackerSlider { get; set; }

	public bool IsSearchTracker { get; set; }

	public bool AddSearchButton { get; set; }

	public List<DashboardViewColumns> DashboardViewColumns { get; set; }

	public List<DashboardViewImageColumns> DashboardViewImageColumns { get; set; }

	public List<DashboardViewFilter> DashboardViewFilters { get; set; }

	public DashBoardExecutionSetting ExecutionSetting { get; set; }

	public string DataViewName { get; set; }

	public List<BAQWhereExpression> ClientFilter { get; set; }

	public DocumentElement DocumentElement { get; set; }

	public DynamicQueryDefinition DynamicQueryDefinition { get; set; }

	public List<AddNewSubscriber> Subscribers { get; }

	public List<CallContextSubscriber> CallContextSubscribers { get; }

	public string GridCaption { get; set; } = string.Empty;

	public bool IsChart { get; set; }

	public bool IsGauge { get; set; }

	internal DashboardViewArgs()
	{
		ClientFilter = new List<BAQWhereExpression>();
		Subscribers = new List<AddNewSubscriber>();
		CallContextSubscribers = new List<CallContextSubscriber>();
	}
}
