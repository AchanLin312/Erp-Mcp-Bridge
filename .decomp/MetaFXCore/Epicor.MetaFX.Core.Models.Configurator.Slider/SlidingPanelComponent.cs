using System.Collections.Generic;

namespace Epicor.MetaFX.Core.Models.Configurator.Slider;

public class SlidingPanelComponent
{
	public bool ShowButtons { get; set; }

	public List<SliderButton> Buttons { get; set; }

	public SlidingPanelComponent()
	{
		Buttons = new List<SliderButton>();
	}
}
