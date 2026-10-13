using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;

namespace Epicor.MetaFX.Core.AppStudio.Metadata;

internal class RowRuleMetadata : BaseMetadataType
{
	public RowRuleMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "rules", contentLoader, cacheService)
	{
	}
}
