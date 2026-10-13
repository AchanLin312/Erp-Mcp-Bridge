namespace Ice.Lib.MetaFX;

/// <summary>
///     EpMetaFx Generate View Request
/// </summary>
public class EpMetaFxGenerateViewRequest
{
	/// <summary>
	///     Type - Can be BAQReport or Dashboard
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	///     Id
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	///     Company
	/// </summary>
	public string Company { get; set; }

	/// <summary>
	///     ProductId Property Ex: EP
	/// </summary>
	public string ProductId { get; set; }

	/// <summary>
	///     GlbCompany Property
	/// </summary>
	public string GlbCompany { get; set; }

	/// <summary>
	///     CgCCode Property
	/// </summary>
	public string CgCCode { get; set; }

	/// <summary>
	///     Layout Type Ex. (Responsive | Absolute)
	/// </summary>
	public string LayoutType { get; set; }

	/// <summary>
	///     List of view panes contaning relevant information to position them.
	/// </summary>
	public ControlPane[] ControlPanes { get; set; }
}
