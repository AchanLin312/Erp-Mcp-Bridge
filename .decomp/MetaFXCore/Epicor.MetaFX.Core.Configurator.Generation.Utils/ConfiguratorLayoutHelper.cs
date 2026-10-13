using System;
using System.Collections.Generic;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;
using Epicor.MetaFX.Core.Models.Configurator.Slider;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;

namespace Epicor.MetaFX.Core.Configurator.Generation.Utils;

public class ConfiguratorLayoutHelper
{
	private readonly ILogger logger;

	public ConfiguratorLayoutHelper(ILogger logger)
	{
		this.logger = logger;
	}

	public Component CreateCheckBoxControl(PcInput control, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-check-box",
			Model = new ErpConfiguratorCheckBox
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				EpBinding = epBinding,
				TabIndex = control.TabOrder,
				Hidden = control.Invisible,
				Disabled = control.ReadOnly,
				Readonly = false,
				IsKeyField = false,
				InitialValue = control.InitVal,
				Required = control.Required,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				UseInitialValue = control.UseInitVal,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseLabel = control.UseScreenLabel
			}
		};
	}

	public Component CreateNumericControl(PcInput control, string epBinding, bool nullable, string format = "-nn", string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-numeric-box",
			Model = new ErpConfiguratorNumericBox
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				Hidden = control.Invisible,
				IsKeyField = false,
				TabIndex = control.TabOrder,
				EpBinding = epBinding,
				Disabled = control.ReadOnly,
				Readonly = false,
				Mask = format,
				Nullable = nullable,
				InitialValue = control.InitVal,
				Required = control.Required,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				Step = control.IncrPrec,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseLabel = control.UseScreenLabel,
				UseInitialValue = control.UseInitVal,
				UseIncremental = control.UseIncrValue,
				UseMaximumValue = control.UseMaxValue,
				UseMinimumValue = control.UseMinValue
			}
		};
	}

	public Component CreateButtonControl(PcInput control, int buttonStyle = 0, string parentId = null, string epBinding = "")
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-button",
			Model = new ErpConfiguratorButton
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				EpBinding = epBinding,
				TabIndex = 0,
				LabelText = control.SideLabel,
				Disabled = false,
				Readonly = false,
				Hidden = control.Invisible,
				IsKeyField = true,
				Style = buttonStyle,
				Tooltip = control.StatusText
			}
		};
	}

	public SliderButton CreateSliderButtonControl(string caption, string uniqueName, int buttonStyle = 0)
	{
		return new SliderButton
		{
			Id = uniqueName,
			Guid = Guid.NewGuid().ToString(),
			EpBinding = "",
			TabIndex = 0,
			LabelText = caption,
			Disabled = false,
			Readonly = false,
			Hidden = false,
			IsKeyField = true,
			Style = buttonStyle
		};
	}

	public Component CreateTextBoxControl(PcInput control, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-text-box",
			Model = new ErpConfiguratorTextbox
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				Hidden = control.Invisible,
				IsKeyField = false,
				EpBinding = epBinding,
				Disabled = control.ReadOnly,
				Readonly = false,
				TabIndex = control.TabOrder,
				Multiline = false,
				Format = control.FormatString,
				InitialValue = control.InitVal,
				Required = control.Required,
				UseInitialValue = control.UseInitVal,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseLabel = control.UseScreenLabel
			}
		};
	}

	public Component CreateDatePickerControl(PcInput control, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-date-picker",
			Model = new ErpConfiguratorDatePicker
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				EpBinding = epBinding,
				TabIndex = control.TabOrder,
				Hidden = control.Invisible,
				IsKeyField = false,
				Disabled = control.ReadOnly,
				Readonly = false,
				InitialValue = control.InitVal,
				Required = control.Required,
				MaximumValue = control.StartDate,
				MinimumValue = control.EndDate,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseInitialValue = control.UseInitVal,
				UseLabel = control.UseScreenLabel,
				UseMaximumValue = control.UseMaxValue,
				UseMinimumValue = control.UseMinValue
			}
		};
	}

	public Component CreateRadioButtonControl(PcInput control, string epBinding, string parentId = null, bool vAlign = false)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-radio-button",
			Model = new ErpConfiguratorRadioButton
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				EpBinding = epBinding,
				VerticalAlign = vAlign,
				TabIndex = control.TabOrder,
				Hidden = control.Invisible,
				Disabled = control.ReadOnly,
				Readonly = false,
				IsKeyField = false,
				Horizontal = control.Horizontal,
				InitialValue = control.InitVal,
				Required = control.Required,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				DynamicAttributeId = control.AttributeId,
				UselistItem = control.UseListValues,
				ExternalRef = control.ExternalRef
			}
		};
	}

	public Component CreateErpWebsiteControl(PcInput control, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-website",
			Model = new ErpConfiguratorWebsite
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				Url = "{" + epBinding + "}",
				LabelText = control.SideLabel,
				TabIndex = control.TabOrder,
				Hidden = control.Invisible,
				Disabled = control.ReadOnly,
				Readonly = false,
				InitialValue = control.InitVal,
				Required = control.Required,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseInitialValue = control.UseInitVal,
				WidgetHeight = control.PHeight,
				WidgetWidth = control.PWidth,
				Capabilities = new List<Capability>
				{
					new Capability
					{
						Id = "IInterpolateUrl",
						Description = "Website widget cap"
					}
				}
			}
		};
	}

	public Component CreateTextAreaControl(PcInput control, string epBinding, int rows, int cols, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-text-area",
			Model = new ErpConfiguratorTextArea
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				Hidden = control.Invisible,
				IsKeyField = false,
				EpBinding = epBinding,
				Disabled = control.ReadOnly,
				Readonly = false,
				TabIndex = control.TabOrder,
				Rows = rows,
				Cols = cols,
				InitialValue = control.InitVal,
				Required = control.Required,
				UseInitialValue = control.UseInitVal,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseLabel = control.UseScreenLabel
			}
		};
	}

	public Component CreateComboBoxControl(PcInput control, string epBinding, string parentId = null)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = parentId,
			SourceTypeId = "erp-combo-box",
			Model = new ErpConfiguratorComboBox
			{
				Id = control.InputName,
				Guid = Guid.NewGuid().ToString(),
				LabelText = control.SideLabel,
				EpBinding = epBinding,
				TabIndex = control.TabOrder,
				Hidden = false,
				Disabled = control.ReadOnly,
				Readonly = false,
				IsKeyField = false,
				InitialValue = control.InitVal,
				Required = control.Required,
				UseInitialValue = control.UseInitVal,
				HideFromSummary = control.NoDispSummary,
				SummaryLabel = control.SummaryLabel,
				Tooltip = control.StatusText,
				GlobalInputVariable = control.GlobalInputVar,
				GlobalInputVariableName = control.GlbInputVarName,
				PublishToDoc = control.IsGlobal,
				AutoSizeList = control.AutoSizeList,
				ListWidth = control.ListWidth,
				ShowDistinct = control.ShowDistinct,
				DynamicAttributeId = control.AttributeId,
				ExternalRef = control.ExternalRef,
				UseLabel = control.UseScreenLabel,
				UseListItem = control.UseListValues
			}
		};
	}

	public Component CreatePictureBox(PcInput control, string epBinding)
	{
		return new Component
		{
			Id = control.InputName,
			ParentId = null,
			SourceTypeId = "erp-picture-box",
			Model = new ErpConfiguratorPictureBox
			{
				Id = control.InputName,
				LabelText = control.SideLabel,
				Hidden = control.Invisible,
				EpBinding = epBinding,
				ImageType = 0,
				ImageFillType = 1,
				Guid = Guid.NewGuid().ToString(),
				InitialValue = control.InitVal
			}
		};
	}
}
