using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-radio-button")]
public class ErpConfiguratorRadioButton : ErpRadioButton
{
	public bool Horizontal { get; set; }

	public bool UselistItem { get; set; }

	public string InitialValue { get; set; }

	public bool Required { get; set; }

	public bool HideFromSummary { get; set; }

	public string SummaryLabel { get; set; }

	public bool GlobalInputVariable { get; set; }

	public string GlobalInputVariableName { get; set; }

	public bool PublishToDoc { get; set; }

	public string DynamicAttributeId { get; set; }

	public bool ExternalRef { get; set; }
}
