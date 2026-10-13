using System;
using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Dashboard;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.Enums;
using Epicor.MetaFX.Core.Models.Layout.EpControls;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Epicor.MetaFX.Core.Helper;

public class LayoutHelper
{
	public const string COMPONENT_REPORTOPTIONS = "ReportOptions";

	public const string COMPONENT_FILTERSUMMARY = "FilterSummary";

	public const string COMPONENT_ADVANCED = "Advanced";

	public const string COMPONENT_SUBMISSION = "Submission";

	private readonly ILogger logger;

	private JsonSerializerSettings jsonSerializerSettings;

	public LayoutHelper(ILogger logger)
	{
		jsonSerializerSettings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			NullValueHandling = NullValueHandling.Ignore
		};
		this.logger = logger;
	}

	public Component CreateCheckBoxControl(string uniqueName, string caption, string epBinding, int tabIndex, bool isReadOnly = false, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-check-box",
			Model = new ErpCheckBox
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				EpBinding = epBinding,
				TabIndex = tabIndex,
				Hidden = false,
				Disabled = false,
				Readonly = isReadOnly,
				IsKeyField = false
			}
		};
	}

	public Component CreateNumericControl(string uniqueName, string caption, string epBinding, int tabIndex, bool nullable, bool isReadOnly = false, string format = "-nn", string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-numeric-box",
			Model = new ErpNumericBox
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				Hidden = false,
				IsKeyField = false,
				TabIndex = tabIndex,
				EpBinding = epBinding,
				Disabled = false,
				Readonly = isReadOnly,
				Mask = format,
				Nullable = nullable
			}
		};
	}

	public Component CreateButtonControl(string caption, string uniqueName, int buttonStyle = 0, string parentId = null, string epBinding = "")
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-button",
			Model = new ErpButton
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				EpBinding = epBinding,
				TabIndex = 0,
				LabelText = caption,
				Disabled = false,
				Readonly = false,
				Hidden = false,
				IsKeyField = true,
				Style = buttonStyle
			}
		};
	}

	public Component CreateTextBoxControl(string uniqueName, string caption, string epBinding, int tabIndex, bool isReadOnly = false, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-text-box",
			Model = new ErpTextBox
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				Hidden = false,
				IsKeyField = false,
				EpBinding = epBinding,
				Disabled = false,
				Readonly = isReadOnly,
				TabIndex = tabIndex,
				Multiline = false
			}
		};
	}

	public Component CreateDateControl(string uniqueName, string caption, string epBinding, int tabIndex, string epBindingToken, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-token-date",
			Model = new ErpTokenDate
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				Hidden = false,
				LabelText = caption,
				EpBinding = epBinding,
				IsKeyField = false,
				Disabled = false,
				Readonly = false,
				TabIndex = tabIndex,
				EpBindingToken = epBindingToken,
				Svc = string.Empty
			}
		};
	}

	public Component CreateDatePickerControl(string uniqueName, string caption, string epBinding, int tabIndex, bool isReadOnly = false, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-date-picker",
			Model = new ErpDatePicker
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				EpBinding = epBinding,
				TabIndex = tabIndex,
				Hidden = false,
				IsKeyField = false,
				Disabled = false,
				Readonly = isReadOnly
			}
		};
	}

	public Component CreateRadioButtonControl(string uniqueName, string caption, string epBinding, int tabIndex, bool isReadOnly = false, string parentId = null, bool vAlign = false)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-radio-button",
			Model = new ErpRadioButton
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				EpBinding = epBinding,
				VerticalAlign = vAlign,
				TabIndex = tabIndex,
				Hidden = false,
				Disabled = false,
				Readonly = isReadOnly,
				IsKeyField = false
			}
		};
	}

	public Component CreateTextAreaControl(string uniqueName, string caption, string epBinding, int tabIndex, int rows, int cols, bool isReadOnly = false, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-text-area",
			Model = new ErpTextArea
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				Hidden = false,
				IsKeyField = false,
				EpBinding = epBinding,
				Disabled = false,
				Readonly = isReadOnly,
				TabIndex = tabIndex,
				Rows = rows,
				Cols = cols
			}
		};
	}

	public Component CreateComboBoxControl(string uniqueName, string caption, string epBinding, int tabIndex, bool isReadOnly = false, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-combo-box",
			Model = new ErpComboBox
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				EpBinding = epBinding,
				TabIndex = tabIndex,
				Hidden = false,
				Disabled = false,
				Readonly = isReadOnly,
				IsKeyField = false
			}
		};
	}

	public Component CreateLabelControl(string uniqueName, string caption, string epBinding, int tabIndex, bool invisible, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "ep-label",
			Model = new EpLabel
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = caption,
				Hidden = invisible,
				IsKeyField = false,
				EpBinding = epBinding,
				Disabled = false,
				TabIndex = tabIndex,
				TextHAlign = "Right",
				TextVAlign = "Middle"
			}
		};
	}

	public Component CreateRowNavigatorControl(string caption, string uniqueName, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "ep-row-navigator",
			Model = new EpRowNavigator
			{
				Id = uniqueName,
				Guid = Guid.NewGuid().ToString(),
				EpBinding = epBinding,
				LabelText = caption,
				Disabled = false,
				Readonly = false,
				Hidden = false
			}
		};
	}

	public Component CreateRow(List<Component> controls, string parentId = null)
	{
		string text = "CONTROL_ROW-" + Guid.NewGuid().ToString().Substring(0, 5);
		string text2 = "col-" + Guid.NewGuid().ToString().Substring(0, 5);
		foreach (Component control in controls)
		{
			control.ParentId = text2;
		}
		return new Component
		{
			Id = text,
			ParentId = parentId,
			SourceTypeId = "metafx-div",
			Model = new MetaFxDiv
			{
				Id = text,
				Class = "row",
				Hidden = false,
				FlexBasis = "50%",
				Guid = Guid.NewGuid().ToString()
			},
			Components = new List<Component>
			{
				new Component
				{
					Id = text2,
					ParentId = text,
					SourceTypeId = "metafx-div",
					Model = new MetaFxDiv
					{
						Id = text2,
						Class = "col",
						Hidden = false,
						FlexBasis = "50%",
						Guid = Guid.NewGuid().ToString()
					},
					Components = controls
				}
			}
		};
	}

	public Component CreateRow(string orientation = "", string parentId = null, string rowID = null)
	{
		string text = ((rowID != null) ? rowID : Guid.NewGuid().ToString().Substring(0, 5));
		string id = "CONTROL_ROW-" + text;
		return new Component
		{
			Id = id,
			ParentId = parentId,
			SourceTypeId = "metafx-div",
			Components = new List<Component>(),
			Model = new MetaFxDiv
			{
				Id = id,
				Class = "row",
				Guid = Guid.NewGuid().ToString(),
				Orientation = orientation,
				Hidden = false,
				FlexBasis = "50%"
			}
		};
	}

	public List<Component> CreateChartPanel(DashboardChart dasboardChartSettings, string parentId, EpAppWidget widget)
	{
		List<Component> list = new List<Component>();
		string text = Guid.NewGuid().ToString();
		Component item = new Component
		{
			Id = text,
			ParentId = parentId,
			SourceTypeId = "metafx-div",
			Model = new MetaFxDiv
			{
				Guid = text,
				Id = "div-row-" + text.Substring(0, 5)
			}
		};
		list.Add(item);
		Component component = CreateColumn("100%", text);
		list.Add(component);
		list.Add(CreateEmbeddedModel(dasboardChartSettings, component.Id, widget));
		return list;
	}

	public Component CreateEmbeddedModel(DashboardChart dasboardChartSettings, string parentID, EpAppWidget embeddedWiget)
	{
		string text = "ep-embedded-app-" + Guid.NewGuid().ToString().Substring(0, 5);
		Component obj = new Component
		{
			Id = text,
			ParentId = parentID,
			SourceTypeId = "ep-embedded-app"
		};
		EpEmbeddedApp obj2 = new EpEmbeddedApp
		{
			Id = text,
			Guid = text,
			WidgetType = "EDD-Chart-View",
			WidgetHeight = 500,
			EmbeddedAppWidget = new EpAppWidget
			{
				Id = embeddedWiget.Id,
				Description = embeddedWiget.Description,
				IconClass = embeddedWiget.IconClass,
				Url = embeddedWiget.Url,
				SetupUrl = embeddedWiget.SetupUrl,
				Capabilities = embeddedWiget.Capabilities,
				Model = new EpAppWidgetModel
				{
					Id = dasboardChartSettings.Id.ToString(),
					BaqId = dasboardChartSettings.BAQ,
					Source = "EDD-CHART-WIDGET-SETUP",
					Action = "add"
				}
			},
			LabelText = dasboardChartSettings.ChartCaption,
			IconClass = embeddedWiget.IconClass
		};
		object[] capabilities = embeddedWiget.Capabilities;
		obj2.Capabilities = capabilities;
		obj.Model = obj2;
		return obj;
	}

	internal MetaFxTwoColumn CreateTwoColumnComponent(string parentId = null, string trackerID = null)
	{
		string text = ((trackerID != null) ? trackerID : Guid.NewGuid().ToString());
		string id = ((trackerID != null) ? (trackerID + "col1") : Guid.NewGuid().ToString());
		string id2 = ((trackerID != null) ? (trackerID + "col2") : Guid.NewGuid().ToString());
		return new MetaFxTwoColumn
		{
			RowComponent = new Component
			{
				Id = text,
				ParentId = parentId,
				SourceTypeId = "metafx-div",
				Model = new MetaFxTwoColumnComponentModel
				{
					Id = "div-row-" + text.Substring(0, 5),
					Guid = text,
					Columns = 2
				}
			},
			Column1 = CreateTwoColumn(id, text),
			Column2 = CreateTwoColumn(id2, text)
		};
	}

	private Component CreateTwoColumn(string id, string parentId)
	{
		return new Component
		{
			Id = id,
			ParentId = parentId,
			SourceTypeId = "metafx-col",
			Model = new ComponentModel
			{
				Id = "div-col-" + id.Substring(0, 5),
				Guid = id,
				FlexBasis = "50%"
			}
		};
	}

	public Component CreateColumn(string flexBasis, string parentId = null, string columnId = null)
	{
		string text = ((columnId != null) ? columnId : Guid.NewGuid().ToString().Substring(0, 5));
		string id = "col-" + text;
		return new Component
		{
			Id = id,
			ParentId = parentId,
			SourceTypeId = "metafx-col",
			Components = new List<Component>(),
			Model = new MetaFxDiv
			{
				Id = id,
				Class = "col",
				Guid = Guid.NewGuid().ToString(),
				Hidden = false,
				FlexBasis = flexBasis
			}
		};
	}

	public Component CreatePanelCard(string caption, string uniqueName, bool expandPanelBar)
	{
		return new Component
		{
			ParentId = null,
			Id = uniqueName,
			SourceTypeId = "metafx-panel-card",
			Model = new MetaFxPanelCard
			{
				Guid = Guid.NewGuid().ToString(),
				Id = uniqueName,
				Class = "PanelCard",
				Title = caption,
				Hidden = false,
				DisabledToolBar = true,
				Expanded = expandPanelBar,
				HighLightPanelBar = true,
				ExpandMore = false,
				EnableActionButton = false,
				BodyComponents = new List<Component>()
			}
		};
	}

	public Component CreatePanelCardGrid(string caption, string uniqueName, bool expandPanelBar, List<Columns> columns, string epBinding = "", bool disableToolBar = true, bool autoFillContainer = false, bool fullScreenButton = false)
	{
		return new Component
		{
			ParentId = null,
			Id = uniqueName,
			SourceTypeId = "metafx-panel-card-grid",
			Model = new MetaFxPanelCardGrid
			{
				Guid = Guid.NewGuid().ToString(),
				Id = uniqueName,
				GridModel = new GridModel
				{
					Columns = columns.ToArray(),
					EpBinding = epBinding
				},
				Title = caption,
				Hidden = false,
				DisabledToolBar = disableToolBar,
				Expanded = expandPanelBar,
				AutoFillContainer = autoFillContainer,
				FullScreenButtonVisible = fullScreenButton
			}
		};
	}

	public Component CreateGroupBox(string uniqueName, string caption, string epBinding, bool invisible, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "GroupBox",
			Model = new ComponentModel
			{
				Id = uniqueName,
				LabelText = caption,
				Hidden = invisible,
				EpBinding = epBinding,
				Guid = Guid.NewGuid().ToString()
			}
		};
	}

	public Component CreateMetaFxBox(string uniqueName, string caption, string epBinding, bool invisible, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "metafx-box",
			Model = new MetaFxBox
			{
				Id = uniqueName,
				LabelText = caption,
				Hidden = invisible,
				EpBinding = epBinding,
				Guid = Guid.NewGuid().ToString()
			}
		};
	}

	public Component CreatePictureBox(string uniqueName, string caption, string epBinding, bool invisible, PictureBoxImageType imageType = PictureBoxImageType.ImageBlob, string parentId = null)
	{
		return new Component
		{
			Id = uniqueName,
			ParentId = parentId,
			SourceTypeId = "erp-picture-box",
			Model = new ErpPictureBox
			{
				Id = uniqueName,
				LabelText = caption,
				Hidden = invisible,
				EpBinding = epBinding,
				ImageType = (int)imageType,
				ImageFillType = 1,
				Guid = Guid.NewGuid().ToString()
			}
		};
	}

	public virtual JArray ConvertToJArray<T>(List<T> obj)
	{
		return JArray.Parse(JsonConvert.SerializeObject(obj, jsonSerializerSettings));
	}

	public JObject ConvertToJObject<T>(T obj)
	{
		return JObject.Parse(JsonConvert.SerializeObject(obj, jsonSerializerSettings));
	}

	public T ConvertToModel<T>(T obj)
	{
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
	}
}
