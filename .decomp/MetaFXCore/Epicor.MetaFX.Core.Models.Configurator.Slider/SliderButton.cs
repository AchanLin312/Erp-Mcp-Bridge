using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Models.Configurator.Slider;

[ComponentModel("cfg-slider-button")]
public class SliderButton : ErpButton
{
	public string DialogResult { get; set; }
}
