namespace Epicor.MetaFX.Core.Models.Configurator;

public class ResponseParameter
{
	public string Name { get; set; }

	public string Type { get; set; }

	public int Sequence { get; set; }

	public bool IsRef { get; set; }

	public bool IsReturnParameter { get; set; }
}
