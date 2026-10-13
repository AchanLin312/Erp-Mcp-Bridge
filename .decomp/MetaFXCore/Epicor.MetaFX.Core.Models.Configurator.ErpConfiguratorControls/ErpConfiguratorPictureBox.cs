using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;

[ComponentModel("cfg-picture-box")]
public class ErpConfiguratorPictureBox : ErpPictureBox
{
	public string InitialValue { get; set; }
}
