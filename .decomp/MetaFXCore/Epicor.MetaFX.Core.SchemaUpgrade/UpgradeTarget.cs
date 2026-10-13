using Epicor.MetaFX.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core.SchemaUpgrade;

internal abstract class UpgradeTarget
{
	protected readonly ILogger logger;

	protected UpgradeTarget(ILogger logger)
	{
		this.logger = logger;
	}

	protected JObject Deserialize(object root)
	{
		DefaultContractResolver contractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};
		return JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(root, new JsonSerializerSettings
		{
			ContractResolver = contractResolver,
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		}));
	}

	public abstract JObject Upgrade(JObject json);

	protected bool IsComment(JProperty p)
	{
		return p?.Name?.StartsWith("_comment_") == true;
	}
}
