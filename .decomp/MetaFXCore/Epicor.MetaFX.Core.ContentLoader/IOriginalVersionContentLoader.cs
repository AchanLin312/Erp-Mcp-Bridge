namespace Epicor.MetaFX.Core.ContentLoader;

public interface IOriginalVersionContentLoader : IContentLoader
{
	bool OriginalVersionMetaUIExists(string appId = null);
}
