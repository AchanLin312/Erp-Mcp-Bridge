using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Dashboard;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Epicor.MetaFX.Core.TemplateManager;

namespace Epicor.MetaFX.Core.Models.Dashboard;

internal class DashboardArgs
{
	public HelperMethods HelperMethods { get; set; }

	public Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade SchemaUpgrade { get; set; }

	public LayoutHelper LayoutHelper { get; set; }

	public DashboardHelper DashboardHelper { get; set; }

	public Epicor.MetaFX.Core.TemplateManager.TemplateManager TemplateManager { get; set; }

	public ILogger Logger { get; set; }

	public DashboardDefinition DashboardDefinition { get; set; }

	public DashboardDataSet DashboardDataSet { get; set; }

	public bool IsOnlyOnePanel { get; set; }

	public string ImportPath { get; set; }

	public string OutputPath { get; set; }

	public object LayoutTemplate { get; set; }

	public string RootPath { get; set; }
}
