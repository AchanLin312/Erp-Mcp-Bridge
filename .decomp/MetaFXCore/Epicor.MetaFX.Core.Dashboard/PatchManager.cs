using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.MetaUI.Patch;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Dashboard;

internal class PatchManager
{
	private readonly DashboardArgs dashboardArgs;

	public PatchManager(DashboardArgs dashboardArgs)
	{
		this.dashboardArgs = dashboardArgs;
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
		return new object[4]
		{
			new
			{
				id = "svc",
				value = "Ice.BO.DynamicQuerySvc"
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
				id = "maintProgram",
				value = string.Empty
			}
		};
	}

	public void CreateFile(PatchSchema patchSchema)
	{
		JObject data = dashboardArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(patchSchema), "patch");
		dashboardArgs.HelperMethods.CreateJsonFile(data, dashboardArgs.OutputPath, "patch.jsonc");
	}
}
