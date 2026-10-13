using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.MetaUI.Patch;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

internal class PatchManager
{
	private readonly ConfiguratorArgs configuratorArgs;

	public PatchManager(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
	}

	public PatchSchema GenerateSchema()
	{
		PatchSchema patchSchema = new PatchSchema();
		patchSchema.Schema = "https://schemas.developer.epicor.com/schemas/v2.0/patch.json";
		patchSchema.ContentVersion = "2.0";
		patchSchema.Imports = new object[0];
		patchSchema.Patch = CreatePatch();
		return patchSchema;
	}

	private object CreatePatch()
	{
		return new object[5]
		{
			new
			{
				id = "svc",
				value = "Erp.BO.ConfigurationDesignSvc"
			},
			new
			{
				id = "param",
				value = string.Empty
			},
			new
			{
				id = "paramModel",
				value = string.Empty
			},
			new
			{
				id = "configID",
				value = configuratorArgs.ConfiguratorId
			},
			new
			{
				id = "maintProgram",
				value = string.Empty
			}
		};
	}

	public void CreateFile(PatchSchema patchSchema)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(patchSchema), "patch");
		configuratorArgs.HelperMethods.CreateJsonFile(data, configuratorArgs.OutputPath, "patch.jsonc");
	}
}
