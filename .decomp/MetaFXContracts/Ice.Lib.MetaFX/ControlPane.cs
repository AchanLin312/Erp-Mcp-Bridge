using System.Drawing;

namespace Ice.Lib.MetaFX;

/// <summary>
///     Control Pane class. 
/// </summary>
public class ControlPane
{
	/// <summary>
	///     Control pane id. This should match with ViewID property.
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	///     Real pane screen location.
	/// </summary>
	public Point Location { get; set; }

	/// <summary>
	///     The position of the pane in the tab component. Only if pane is child of a TabGroup.
	/// </summary>
	public int? TabIndex { get; set; }

	/// <summary>
	///     The parent tab id. Only if pane is child of a TabGroup.
	/// </summary>
	public int? TabId { get; set; }

	/// <summary>
	///     Whether pane is currently selected. Only if pane is child of a TabGroup.
	/// </summary>
	public bool? IsSelectedTab { get; set; }
}
