using System.Collections.Generic;

namespace Ice.Lib.MetaFX;

/// <summary>
///     MetaFx Response.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EpMetaFxResponse<T>
{
	/// <summary>
	///     Data
	/// </summary>
	public T Data;

	/// <summary>
	///     True in case of success .
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	///     Message
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	///     Detail Messages
	/// </summary>
	public List<MessageDetail> Details { get; set; }

	/// <summary>
	///     Constructor.
	/// </summary>
	public EpMetaFxResponse()
	{
		Details = new List<MessageDetail>();
	}
}
