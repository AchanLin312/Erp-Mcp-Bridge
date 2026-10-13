namespace Epicor.MetaFX.Core.Models.Layout;

public class ComponentBoxData
{
	public static readonly int MINIMUM_ROW_SIZE = 3;

	public static readonly int MINIMUM_COLUMN_SIZE = 6;

	public int X { get; set; }

	public int Y { get; set; }

	public int H { get; set; }

	public int W { get; set; }

	public int? MaxSharedComponentsInRow { get; set; }

	public int? MaxSharedComponentsInColumn { get; set; }

	public ComponentBoxData()
	{
		X = 1;
		Y = 1;
		H = MINIMUM_ROW_SIZE;
		W = MINIMUM_COLUMN_SIZE;
	}
}
