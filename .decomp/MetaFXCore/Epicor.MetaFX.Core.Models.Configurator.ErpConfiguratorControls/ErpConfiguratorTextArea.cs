using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-text-area")]
public class ErpConfiguratorTextArea : ErpTextArea
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
}
