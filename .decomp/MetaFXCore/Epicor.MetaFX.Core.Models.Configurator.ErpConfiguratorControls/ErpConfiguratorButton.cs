using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-erp-button")]
public class ErpConfiguratorButton : ErpButton
{
	public string Tooltip { get; set; }
}
