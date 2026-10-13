namespace Epicor.MetaFX.Core.Models;

public class ConversionLogMessageDetail : MessageDetail
{
	public string MethodName { get; set; }

	public bool IsEvent { get; set; }
}
