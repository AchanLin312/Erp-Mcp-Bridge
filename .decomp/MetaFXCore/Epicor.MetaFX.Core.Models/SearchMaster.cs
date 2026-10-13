namespace Epicor.MetaFX.Core.Models;

public class SearchMaster
{
	public int Id { get; set; }

	public string Type { get; set; }

	public string Text { get; set; }

	public object Schema { get; set; }

	public bool IsDefault { get; set; }
}
