using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-numeric-box")]
public class ErpConfiguratorNumericBox : ErpNumericBox
{
	public string InitialValue { get; set; }

	public bool Required { get; set; }

	public bool UseInitialValue { get; set; }

	public bool HideFromSummary { get; set; }

	public string SummaryLabel { get; set; }

	public string Tooltip { get; set; }

	public bool GlobalInputVariable { get; set; }

	public string GlobalInputVariableName { get; set; }

	public bool PublishToDoc { get; set; }

	public string DynamicAttributeId { get; set; }

	public bool ExternalRef { get; set; }

	public bool UseLabel { get; set; }

	public bool UseIncremental { get; set; }

	public bool UseMaximumValue { get; set; }

	public bool UseMinimumValue { get; set; }
}
