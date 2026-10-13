namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

public class ActionV11
{
	public string type { get; set; }

	public object param { get; set; }

	public object onSuccess { get; set; }

	public object onError { get; set; }

	public string value { get; set; }

	public object option { get; set; }

	public string epBinding { get; set; }

	public string dataview { get; set; }
}
