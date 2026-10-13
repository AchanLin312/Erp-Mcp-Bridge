using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models;

public class EpMetaFxResponse<T>
{
	public T Data;

	public bool IsSuccess { get; set; }

	public string Message { get; set; }

	public List<MessageDetail> Details { get; set; }

	public EpMetaFxResponse()
	{
		Details = new List<MessageDetail>();
	}
}
