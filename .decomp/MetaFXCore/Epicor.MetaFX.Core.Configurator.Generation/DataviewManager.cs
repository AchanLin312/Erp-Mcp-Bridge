using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.MetaUI;
using Epicor.MetaFX.Core.Models.MetaUI.DataView;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

internal class DataviewManager
{
	private readonly ConfiguratorArgs configuratorArgs;

	public DataviewManager(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
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
					Refs = configuratorArgs.ImportPath + "/dataviews.jsonc#/dataviews"
				}
			}
		};
		dataViewSchema.DataViews = new List<DataView>();
		return dataViewSchema;
	}

	public List<DataView> GenerateDataViews()
	{
		List<DataView> list = new List<DataView>();
		foreach (PcPage item in configuratorArgs.Configuration.PcPage)
		{
			list.Add(new DataView
			{
				Id = configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq),
				Table = configuratorArgs.ConfiguratorHelper.GetDataview(item.PageSeq),
				Schema = "Erp"
			});
		}
		return list;
	}

	public void CreateFile(DataViewSchema dataViewSchema)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(dataViewSchema), "dataviews");
		configuratorArgs.HelperMethods.CreateJsonFile(data, configuratorArgs.OutputPath, "dataviews.jsonc");
	}
}
