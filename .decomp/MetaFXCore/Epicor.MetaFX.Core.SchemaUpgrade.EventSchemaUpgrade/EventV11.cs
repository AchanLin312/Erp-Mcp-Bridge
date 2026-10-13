namespace Epicor.MetaFX.Core.SchemaUpgrade.EventSchemaUpgrade;

public class EventV11
{
	public string id { get; set; }

	public string description { get; set; }

	public object trigger { get; set; }

	public ActionV11 action { get; set; }
}
