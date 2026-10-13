namespace Ice.Lib.MetaFX;

/// <summary>
///     Cache invalidate request
/// </summary>
public class EpMetaFxCacheInvalidateRequest
{
	/// <summary>
	///  Use with <see cref="P:Ice.Lib.MetaFX.EpMetaFxCacheInvalidateRequest.Like" /> property to specify the search form. Optionally specify view identifier for application when Like is not provided. 
	/// </summary>
	public string viewId { get; set; } = string.Empty;

	/// <summary>
	///   Type of cache to clear. Expected values: view (or empty), search
	/// </summary>
	public string Type { get; set; } = string.Empty;

	/// <summary>
	///  Clear the cache for the provided search. If <see cref="P:Ice.Lib.MetaFX.EpMetaFxCacheInvalidateRequest.viewId" /> is specified, cache within that search form is cleared. 
	///  If not, cache for all search forms will be cleared.
	///
	/// </summary>
	public string Like { get; set; } = string.Empty;

	/// <summary>
	/// Clear the cache for the provided Language Id. E.g. 'enu', 'esm' 
	/// </summary>
	public string LanguageId { get; set; } = string.Empty;
}
