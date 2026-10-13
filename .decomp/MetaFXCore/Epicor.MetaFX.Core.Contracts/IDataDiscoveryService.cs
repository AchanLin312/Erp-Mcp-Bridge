using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout.EpControls;

namespace Epicor.MetaFX.Core.Contracts;

public interface IDataDiscoveryService
{
	EpAppWidget GetWidgets();

	void SetPubSubModel(EpAppWidget epAppWidget, List<DashboardChartFilter> filters);

	string GetDataDiscoveryURL();

	DashboardChart SaveDashboardChart(string dataDiscoveyURL, DashboardView dashboardView);
}
