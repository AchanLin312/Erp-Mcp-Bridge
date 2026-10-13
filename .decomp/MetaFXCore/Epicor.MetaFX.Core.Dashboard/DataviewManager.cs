using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

internal class DataviewManager
{
	private readonly DashboardArgs dashboardArgs;

	public DataviewManager(DashboardArgs dashboardArgs)
	{
		this.dashboardArgs = dashboardArgs;
	}

	public DataViewSchema GenerateSchema()
	{
		DataViewSchema dataViewSchema = new DataViewSchema();
		dataViewSchema.Schema = "https://schemas.developer.epicor.com/schemas/v1.0/dataviews.json";
		dataViewSchema.ContentVersion = "1.0";
		dataViewSchema.Imports = new object[1]
		{
			new
			{
				dataviews = new Reference
				{
					Refs = dashboardArgs.ImportPath + "/dataviews.jsonc#/dataviews"
				}
			}
		};
		dataViewSchema.DataViews = new List<DataView>();
		return dataViewSchema;
	}

	public DataView GenerateDataView(DashboardViewArgs dashboardViewArgs)
	{
		return new DataView
		{
			Id = dashboardViewArgs.DataViewName,
			Table = (dashboardViewArgs.IsGrid ? dashboardViewArgs.DataViewName : string.Empty),
			Schema = (dashboardViewArgs.IsGrid ? "baq" : string.Empty),
			Data = (dashboardViewArgs.IsGrid ? dashboardViewArgs.DashboardQuery.DynamicQueryID : null),
			AdditionalColumns = ((dashboardViewArgs.DashboardViewImageColumns != null && dashboardViewArgs.DashboardViewImageColumns.Count > 0) ? dashboardArgs.DashboardHelper.GetImageColumns(dashboardViewArgs) : null)
		};
	}

	public DataView GenerateDataViewForEmbedGrid(DashboardViewArgs dashboardViewArgs)
	{
		return new DataView
		{
			Id = dashboardArgs.DashboardHelper.GetDataviewNameForEmbedGrid(dashboardViewArgs),
			Table = dashboardArgs.DashboardHelper.GetDataviewNameForEmbedGrid(dashboardViewArgs),
			Schema = "baq",
			Data = dashboardViewArgs.DashboardQuery.DynamicQueryID
		};
	}

	public void CreateFile(DataViewSchema dataViewSchema)
	{
		JObject data = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(dataViewSchema), "dataviews");
		dashboardArgs.HelperMethods.CreateJsonFile(data, dashboardArgs.OutputPath, "dataviews.jsonc");
	}
}
