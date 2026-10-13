namespace Epicor.MetaFX.Core._4xLayoutUpgrade;

internal class Constants
{
	public const string SOURCETYPE_PANEL = "Panel";

	public const string SOURCETYPE_GROUPBOX = "GroupBox";

	public const string SOURCETYPE_METAFXTAB = "metafx-tabstrip";

	public const string SOURCETYPE_GRID = "erp-grid";

	public const string SOURCETYPE_COMBOXBOX = "erp-combo-box";

	public const string SOURCETYPE_DIV = "metafx-div";

	public const string PAGENAME = "pageName";

	public const string PANELCARDID = "panelCardId";

	public const string SOURCETYPE_PANELCARDGRID = "metafx-panel-card-grid";

	public const string SOURCETYPE_PANELCARD = "metafx-panel-card";

	public const string SOURCETYPE_LABEL = "ep-label";

	public const string DETAIL_DESIGNER_FILE_NAME = "Details.layout.designer.draft.jsonc";

	public const string ACTIVITY_DESIGNER_FILE_NAME = "Activity.layout.designer.draft.jsonc";

	public const string DETAIL_FILE_NAME = "Details";

	public const string MAIN_FILE_NAME = "Main";

	public const string Activity_FILE_NAME = "Activity";

	public const string SOURCETYPE = "sourceTypeId";

	public const string TARGETTYPE = "targetTypeId";

	public const string SLIDING_PANEL = "SlidingPanel";

	public const string COLON = ":";

	public const string ID = "id";

	public const string TITLE = "title";

	public static readonly string[] ATTRIBUTES_TO_REMOVE = new string[12]
	{
		"column", "isReusable", "type", "subType", "enableHeader", "showOnlyDataView", "enableDropDown", "showHeroBar", "height", "width",
		"top", "left"
	};
}
