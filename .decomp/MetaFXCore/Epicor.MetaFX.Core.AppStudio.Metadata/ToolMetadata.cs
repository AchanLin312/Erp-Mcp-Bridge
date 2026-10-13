using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;

namespace Epicor.MetaFX.Core.AppStudio.Metadata;

internal class ToolMetadata : BaseMetadataType
{
	public ToolMetadata(string rootPath, ILogger logger, IContentLoader contentLoader, ICacheService cacheService)
		: base(rootPath, logger, "tools", contentLoader, cacheService)
	{
	}
}
