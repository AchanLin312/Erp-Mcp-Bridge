using System.IO;
using System.Net;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Events;

public class EventSchema
{
	private const string EVENT_FILE_VERSION = "v5.0";

	private const string EVENT_FILE_PATH = "https://schemas.developer.epicor.com/schemas/{0}/events.json";

	private const string EVENT_FILE_LOCAL_PATH = "Epicor.MetaFX.Core.Events.EventFiles.events.{0}.json";

	public JObject Download(DownloadSchemaRequest request)
	{
		try
		{
			string text = Path.GetTempPath() + "events.json";
			new WebClient().DownloadFile(string.Format("https://schemas.developer.epicor.com/schemas/{0}/events.json", "v5.0"), text);
			return JObject.Parse(File.ReadAllText(text));
		}
		catch
		{
			return GetJsonEmbeddedResourceFile(string.Format("Epicor.MetaFX.Core.Events.EventFiles.events.{0}.json", "v5.0"));
		}
	}

	private JObject GetJsonEmbeddedResourceFile(string filename)
	{
		using StreamReader streamReader = new StreamReader(GetType().Assembly.GetManifestResourceStream(filename));
		return JObject.Parse(streamReader.ReadToEnd());
	}
}
