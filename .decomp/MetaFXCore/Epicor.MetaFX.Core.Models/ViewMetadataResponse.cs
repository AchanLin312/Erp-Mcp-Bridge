using System.Collections.Generic;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.MetaUI.Strings;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models;

public class ViewMetadataResponse
{
	public bool AllowVersions = true;

	public bool PersonalizationNotLoaded;

	public bool HasOrphans;

	public object Layout { get; set; }

	public object Events { get; set; }

	public object ToolBar { get; set; }

	public object DataViews { get; set; }

	public object Rules { get; set; }

	public object References { get; set; }

	public JArray Pages { get; set; }

	public object Properties { get; set; }

	public string Wasm { get; set; }

	public bool IsLayerDisabled { get; set; }

	public ConfiguratorInfo ConfiguratorInfo { get; set; }

	public List<ApplicationString> ApplicationStrings { get; set; }

	public string ServerErrorCode { get; set; }

	public string[] ServerErrors { get; set; }

	public JArray Orphans { get; set; }

	public bool BypassLandingPage { get; set; }
}
