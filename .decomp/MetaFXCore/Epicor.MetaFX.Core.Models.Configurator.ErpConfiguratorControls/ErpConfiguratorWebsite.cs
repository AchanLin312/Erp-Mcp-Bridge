using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-erp-website")]
public class ErpConfiguratorWebsite : ErpWebsite
{
	public string InitialValue { get; set; }

	public bool Required { get; set; }

	public bool HideFromSummary { get; set; }

	public string SummaryLabel { get; set; }

	public string DynamicAttributeId { get; set; }

	public bool ExternalRef { get; set; }

	public bool UseInitialValue { get; set; }
}
