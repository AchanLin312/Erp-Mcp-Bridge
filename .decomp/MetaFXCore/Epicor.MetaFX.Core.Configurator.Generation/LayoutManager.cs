using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Configurator.ErpConfiguratorControls;
using Epicor.MetaFX.Core.Models.Configurator.Slider;
using Epicor.MetaFX.Core.Models.Layout;
using Epicor.MetaFX.Core.Models.Layout.ErpControls;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator.Generation;

internal class LayoutManager
{
	private readonly ConfiguratorArgs configuratorArgs;

	private readonly string[] ignoreSideLabelControls = new string[3] { "ep-label", "erp-button", "erp-picture-box" };

	public LayoutManager(ConfiguratorArgs configuratorArgs)
	{
		this.configuratorArgs = configuratorArgs;
	}

	public Dictionary<PcPage, List<Component>> GenerateComponents()
	{
		Dictionary<PcPage, List<Component>> dictionary = new Dictionary<PcPage, List<Component>>();
		foreach (PcPage page in configuratorArgs.Configuration.PcPage)
		{
			string empty = string.Empty;
			Component component = configuratorArgs.LayoutHelper.CreatePanelCard(page.PageTitle, $"PanelCard{page.PageSeq}", expandPanelBar: true);
			List<Component> list = new List<Component>();
			dictionary.Add(page, new List<Component> { component });
			Component component2 = configuratorArgs.LayoutHelper.CreateRow("vertical", component.Id + "#bodyComponents");
			component2.Components = null;
			Component component3 = configuratorArgs.LayoutHelper.CreateColumn("100%", component2.Id);
			component3.Components = null;
			empty = component3.Id;
			dictionary[page].Add(component2);
			dictionary[page].Add(component3);
			foreach (PcInput control in from x in configuratorArgs.Configuration.PcInputs
				where x.PageSeq == page.PageSeq
				orderby x.YPos, x.XPos
				select x)
			{
				string epBinding = configuratorArgs.ConfiguratorHelper.GetDataview(control.PageSeq) + "." + control.InputName;
				Component component4 = null;
				switch (control.ControlType)
				{
				case "Ice.Lib.Framework.EpiButton, Ice.Lib.EpiClientLib":
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateButtonControl(control, 0, null, epBinding);
					break;
				case "Ice.Lib.Framework.EpiGroupBox, Ice.Lib.EpiClientLib":
					component4 = configuratorArgs.LayoutHelper.CreateMetaFxBox(control.InputName, control.SideLabel, epBinding, control.Invisible);
					break;
				case "Ice.Lib.Framework.EpiUltraCombo, Ice.Lib.EpiClientLib":
				{
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateComboBoxControl(control, epBinding);
					ErpConfiguratorComboBox erpConfiguratorComboBox = component4.Model as ErpConfiguratorComboBox;
					erpConfiguratorComboBox.TextField = "display";
					erpConfiguratorComboBox.ValueField = "value";
					if (!string.IsNullOrEmpty(control.ListItems))
					{
						erpConfiguratorComboBox.List = configuratorArgs.ConfiguratorHelper.GetComboFields(control.ListItems);
					}
					if (configuratorArgs.Configuration.PcDynLst.Find((PcDynLst x) => x.InputName == control.InputName) == null)
					{
						break;
					}
					List<DynamicList> list2 = new List<DynamicList>();
					foreach (PcDynLst pcDynLst in configuratorArgs.Configuration.PcDynLst.Where((PcDynLst x) => x.InputName == control.InputName))
					{
						DynamicList dynamicList = new DynamicList();
						PropertyMapper<PcDynLst, DynamicList>.Copy(pcDynLst, dynamicList);
						dynamicList.List = configuratorArgs.ConfiguratorHelper.GetComboFields(pcDynLst.ListItems);
						dynamicList.Parameters = new List<DynLstParam>();
						dynamicList.Criterias = new List<DynLstCriteria>();
						foreach (PcDynLstParam item in configuratorArgs.Configuration.PcDynLstParams.Where((PcDynLstParam x) => x.InputName == pcDynLst.InputName && x.ListSeq == pcDynLst.ListSeq).ToList())
						{
							DynLstParam dynLstParam = new DynLstParam();
							PropertyMapper<PcDynLstParam, DynLstParam>.Copy(item, dynLstParam);
							dynamicList.Parameters.Add(dynLstParam);
						}
						foreach (PcDynLstCriteria item2 in configuratorArgs.Configuration.PcDynLstCriteria.Where((PcDynLstCriteria x) => x.InputName == pcDynLst.InputName && x.ListSeq == pcDynLst.ListSeq).ToList())
						{
							DynLstCriteria dynLstCriteria = new DynLstCriteria();
							PropertyMapper<PcDynLstCriteria, DynLstCriteria>.Copy(item2, dynLstCriteria);
							UpdatePcDynLstCriteriaColumnValue(item2, dynLstCriteria);
							dynamicList.Criterias.Add(dynLstCriteria);
						}
						list2.Add(dynamicList);
					}
					erpConfiguratorComboBox.DynamicLists = list2.ToArray();
					break;
				}
				case "Ice.Lib.Framework.EpiCheckBox, Ice.Lib.EpiClientLib":
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateCheckBoxControl(control, epBinding);
					break;
				case "Ice.Lib.Framework.EpiTextBox, Ice.Lib.EpiClientLib":
				{
					if (control.DesignControlType == "Erp.Lib.Configurator.PcControls.EpiPcTextEditor, Erp.Lib.Configurator")
					{
						component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateTextAreaControl(control, epBinding, Convert.ToInt32(control.PHeight / 20), Convert.ToInt32(control.PWidth / 5));
						break;
					}
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateTextBoxControl(control, epBinding);
					ErpConfiguratorTextbox erpConfiguratorTextbox = component4.Model as ErpConfiguratorTextbox;
					if (!string.IsNullOrEmpty(control.ValList))
					{
						erpConfiguratorTextbox.ListItems = GetValidList(control.ValList);
					}
					break;
				}
				case "Ice.Lib.Framework.EpiLabel, Ice.Lib.EpiClientLib":
					component4 = configuratorArgs.LayoutHelper.CreateLabelControl(control.InputName, control.SideLabel, epBinding, control.TabOrder, control.Invisible);
					break;
				case "Erp.Lib.Configurator.PcControls.EpiRadioSet, Erp.Lib.Configurator":
				{
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateRadioButtonControl(control, epBinding, null, vAlign: true);
					ErpRadioButton erpRadioButton = component4.Model as ErpRadioButton;
					if (!string.IsNullOrEmpty(control.ListItems))
					{
						erpRadioButton.Data = configuratorArgs.ConfiguratorHelper.GetRadioFields(control.ListItems);
					}
					break;
				}
				case "Ice.Lib.Framework.EpiDateTimeEditor, Ice.Lib.EpiClientLib":
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateDatePickerControl(control, epBinding);
					break;
				case "Ice.Lib.Framework.EpiNumericEditor, Ice.Lib.EpiClientLib":
				{
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateNumericControl(control, epBinding, control.StartDec <= 0, string.IsNullOrEmpty(control.FormatString) ? null : control.FormatString);
					ErpNumericBox erpNumericBox = component4.Model as ErpNumericBox;
					if (control.StartDec != 0L || control.EndDec != 0L)
					{
						erpNumericBox.Min = control.StartDec;
						erpNumericBox.Max = control.EndDec;
					}
					break;
				}
				case "Erp.Lib.Configurator.PcControls.EpiPcPictureBox, Erp.Lib.Configurator":
					epBinding = configuratorArgs.ConfiguratorHelper.GetImageBlobName("TransView", control.InputName) ?? "";
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreatePictureBox(control, epBinding);
					component4.Model.Width = control.PWidth;
					component4.Model.Height = control.PHeight;
					break;
				case "Erp.Lib.Configurator.PcControls.EpiPcBrowser, Erp.Lib.Configurator":
					component4 = configuratorArgs.ConfiguratorLayoutHelper.CreateErpWebsiteControl(control, epBinding);
					break;
				}
				if (component4 != null)
				{
					if (control.Invisible)
					{
						component4.Model.Hidden = control.Invisible;
					}
					component4.ParentId = empty;
					list.Add(component4);
				}
			}
			dictionary[page].AddRange(list);
		}
		return dictionary;
	}

	private void UpdatePcDynLstCriteriaColumnValue(PcDynLstCriteria item, DynLstCriteria dynLstCriteria)
	{
		switch (item.ValueFrom?.ToLowerInvariant())
		{
		case "constant":
			switch (item.ColumnType?.ToLowerInvariant())
			{
			case "bit":
			{
				bool.TryParse(item.ColumnValue, out var result2);
				dynLstCriteria.BooleanValue = result2;
				break;
			}
			case "date":
				dynLstCriteria.DateValue = item.ColumnValue;
				break;
			case "decimal":
			{
				double.TryParse(item.ColumnValue, out var result);
				dynLstCriteria.DecimalValue = result;
				break;
			}
			default:
				dynLstCriteria.StringValue = item.ColumnValue;
				break;
			}
			break;
		case "baqcolumn":
			dynLstCriteria.BaqValue = item.ColumnValue;
			break;
		case "inputname":
			dynLstCriteria.InputNameValue = item.ColumnValue;
			break;
		}
	}

	private long CalcLabelLeft(PcInput control, long componentLeft)
	{
		if (control.ControlType == "Ice.Lib.Framework.EpiGroupBox, Ice.Lib.EpiClientLib")
		{
			return componentLeft + 4;
		}
		int num = 15;
		SizeF sizeF;
		using (Graphics graphics = Graphics.FromImage(new Bitmap(1, 1)))
		{
			sizeF = graphics.MeasureString(control.SideLabel, new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel));
		}
		return componentLeft - (Convert.ToInt64(sizeF.Width) + num);
	}

	private long CalcLabelTop(PcInput control)
	{
		double num = 1.2;
		if (control.ControlType == "Ice.Lib.Framework.EpiGroupBox, Ice.Lib.EpiClientLib")
		{
			return Convert.ToInt64((double)control.YPos * num + 0.2);
		}
		return Convert.ToInt64((double)control.YPos * num);
	}

	private JToken GenerateSchema(object layoutTemplate, PcPage page, string pageCaption, string pageName, string viewType = "Configurator", string parentPage = "")
	{
		JToken jToken = JToken.FromObject(layoutTemplate);
		JToken jToken2 = jToken["layout"];
		if (jToken2["components"] is JArray jArray)
		{
			jArray.Clear();
		}
		jToken2["name"] = pageName;
		jToken2["caption"] = ((!string.IsNullOrEmpty(pageCaption)) ? pageCaption : ((page != null) ? configuratorArgs.ConfiguratorHelper.GetDataview(page.PageSeq) : string.Empty));
		jToken2["viewType"] = viewType;
		if (page != null)
		{
			jToken2["width"] = ((page.PWidth + 15 < 800) ? 800 : (page.PWidth + 15));
		}
		else
		{
			jToken2["width"] = 800;
		}
		jToken2["pageType"] = configuratorArgs.LayoutType;
		if (viewType == "Configurator")
		{
			if (configuratorArgs.ExtendedConfiguration.IsMultiPage)
			{
				var obj = new
				{
					items = new object[1]
					{
						new
						{
							type = 2,
							component = configuratorArgs.LayoutHelper.CreateRowNavigatorControl("Page", Guid.NewGuid().ToString(), "[PcPage]")
						}
					}
				};
				jToken2["heroBar"] = configuratorArgs.LayoutHelper.ConvertToJObject(obj);
			}
			jToken2["slidingPanel"] = configuratorArgs.LayoutHelper.ConvertToJObject(CreateSlidingPanelButtons());
		}
		if (viewType == "SlidingPanel")
		{
			jToken2["slidingPanel"] = configuratorArgs.LayoutHelper.ConvertToJObject(CreateSlidingPanelButtons(showButtons: false));
			jToken2["slidingPanel"]["hideClose"] = true;
			jToken2["slidingPanel"]["collapseOnOutsideClick"] = false;
			jToken2["width"] = 967;
		}
		if (viewType == "Tab")
		{
			jToken2["navigation"]["enable"] = true;
		}
		else if (page != null)
		{
			jToken2["readOnly"] = page.ReadOnly;
			jToken2["skipOnLeaveOPE"] = page.SkipOnLeaveOpe;
			jToken2["skipOnLeaveOPL"] = page.SkipOnLeaveOpl;
			jToken2["skipPageNoInputs"] = page.SkipPageNoInputs;
		}
		jToken2["navigation"]["parentPage"] = parentPage;
		jToken2["navigation"]["pageCaption"] = pageCaption;
		return jToken;
	}

	public void CreateLayoutAndPages(object layoutTemplate, Dictionary<PcPage, List<Component>> pageComponents)
	{
		JToken jToken = GenerateSchema(layoutTemplate, configuratorArgs.LayoutPage, configuratorArgs.LayoutPage.PageTitle, configuratorArgs.ConfiguratorHelper.GetDataview(configuratorArgs.LayoutPage.PageSeq));
		jToken["layout"]["components"] = configuratorArgs.LayoutHelper.ConvertToJArray(pageComponents.First().Value);
		CreateFile(jToken);
		if (!Directory.Exists(Path.Combine(configuratorArgs.OutputPath, "pages")))
		{
			Directory.CreateDirectory(Path.Combine(configuratorArgs.OutputPath, "pages"));
		}
		if (!configuratorArgs.ExtendedConfiguration.IsMultiPage)
		{
			CreateLayoutPageForSummaryGrid(layoutTemplate, configuratorArgs.ConfiguratorHelper.GetDataview(configuratorArgs.LayoutPage.PageSeq));
			CreateLayoutPageForSmartString(layoutTemplate, configuratorArgs.ConfiguratorHelper.GetDataview(configuratorArgs.LayoutPage.PageSeq));
			return;
		}
		int num = 0;
		foreach (KeyValuePair<PcPage, List<Component>> pageComponent in pageComponents)
		{
			string dataview = configuratorArgs.ConfiguratorHelper.GetDataview(pageComponent.Key.PageSeq);
			JToken jToken2;
			if (num == 0)
			{
				jToken2 = GenerateSchema(layoutTemplate, pageComponent.Key, "Main", "Tab", "Tab", dataview);
				jToken2["layout"]["components"] = new JArray();
			}
			else
			{
				jToken2 = GenerateSchema(layoutTemplate, pageComponent.Key, pageComponent.Key.PageTitle, dataview, "TabPage", "Tab");
				jToken2["layout"]["components"] = configuratorArgs.LayoutHelper.ConvertToJArray(pageComponent.Value);
			}
			CreatePage(jToken2, dataview);
			num++;
		}
		CreateLayoutPageForSummaryGrid(layoutTemplate, configuratorArgs.ConfiguratorHelper.GetDataview(configuratorArgs.LayoutPage.PageSeq));
		CreateLayoutPageForSmartString(layoutTemplate, configuratorArgs.ConfiguratorHelper.GetDataview(configuratorArgs.LayoutPage.PageSeq));
	}

	private void CreateLayoutPageForSummaryGrid(object layoutTemplate, string parentPage)
	{
		List<Component> obj = CreateComponentsForSummaryGrid();
		string pageName = "ConfigurationSummary";
		JToken jToken = GenerateSchema(layoutTemplate, null, "Configuration Summary", pageName, "SlidingPanel", parentPage);
		jToken["layout"]["components"] = configuratorArgs.LayoutHelper.ConvertToJArray(obj);
		CreatePage(jToken, pageName);
	}

	private List<Component> CreateComponentsForSummaryGrid()
	{
		List<Component> list = new List<Component>
		{
			configuratorArgs.LayoutHelper.CreatePanelCardGrid("Configuration Summary", "ConfigurationSummaryGrid", expandPanelBar: true, GetColumnsForSummaryGrid("ConfigurationSummaryGrid"), "ConfigurationSummary"),
			configuratorArgs.LayoutHelper.CreatePanelCardGrid("Input Summary", "InputSummaryGrid", expandPanelBar: true, GetColumnsForSummaryGrid("InputSummaryGrid"), "PcInputValue")
		};
		GridModel gridModel = (list[1].Model as MetaFxPanelCardGrid).GridModel;
		int height = ((list[0].Model as MetaFxPanelCardGrid).GridModel.Height = 200);
		gridModel.Height = height;
		Component component = configuratorArgs.LayoutHelper.CreateButtonControl("Reconfigure", "btnReconfigure");
		component.Model.EpBinding = "TransView." + component.Id;
		list.Insert(0, component);
		return list;
	}

	private void CreateLayoutPageForSmartString(object layoutTemplate, string parentPage)
	{
		List<Component> obj = CreateComponentsForSmartString();
		string pageName = "SmartString";
		JToken jToken = GenerateSchema(layoutTemplate, null, "Smart String", pageName, "SlidingPanel", parentPage);
		jToken["layout"]["components"] = configuratorArgs.LayoutHelper.ConvertToJArray(obj);
		CreatePage(jToken, pageName);
	}

	private List<Component> CreateComponentsForSmartString()
	{
		List<Component> list = new List<Component>();
		Component component = configuratorArgs.LayoutHelper.CreateTextAreaControl("SmartStringValues", "Smart String Values:", "TransView.InSmartStringValue", 0, 15, 40);
		component.Model.Width = 700L;
		list.Add(component);
		list.Add(CreateOkCancelButtons(list));
		return list;
	}

	private List<Columns> GetColumnsForSummaryGrid(string gridName)
	{
		if (!(gridName == "ConfigurationSummaryGrid"))
		{
			if (gridName == "InputSummaryGrid")
			{
				return new List<Columns>
				{
					new Columns
					{
						Field = "ConfigID",
						Title = "Config ID"
					},
					new Columns
					{
						Field = "DspPageSeq",
						Title = "Display Page Sequence"
					},
					new Columns
					{
						Field = "PageTitle",
						Title = "Page Title"
					},
					new Columns
					{
						Field = "InputName",
						Title = "Input Name"
					},
					new Columns
					{
						Field = "SummaryLabel",
						Title = "Summary Label"
					},
					new Columns
					{
						Field = "InputValue",
						Title = "Input Value"
					},
					new Columns
					{
						Field = "TabOrder",
						Title = "Tab Order"
					}
				};
			}
			return new List<Columns>();
		}
		return new List<Columns>
		{
			new Columns
			{
				Field = "ConfigID",
				Title = "Config ID"
			},
			new Columns
			{
				Field = "BasePartNum",
				Title = "Base Part Number"
			},
			new Columns
			{
				Field = "BaseRevisionNum",
				Title = "Base Revision Number"
			},
			new Columns
			{
				Field = "ConfigBaseUnitPrice",
				Title = "Configuration Base Unit Price"
			},
			new Columns
			{
				Field = "DocConfigBaseUnitPrice",
				Title = "Configuration Doc Base Unit Price"
			},
			new Columns
			{
				Field = "SmartString",
				Title = "Smart String"
			}
		};
	}

	private void CreateFile(object layoutSchema)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(layoutSchema), "layout");
		configuratorArgs.HelperMethods.CreateJsonFile(data, configuratorArgs.OutputPath, "layout.jsonc");
	}

	private void CreatePage(object pageSchema, string pageName)
	{
		JObject data = configuratorArgs.SchemaUpgrade.UpgradeToLatestSchema(JObject.FromObject(pageSchema), "layout");
		configuratorArgs.HelperMethods.CreateJsonFile(data, Path.Combine(configuratorArgs.OutputPath, "pages"), pageName + ".layout.jsonc");
	}

	private ValidListItem[] GetValidList(string validListStr)
	{
		string[] array = validListStr.Split(new char[1] { '~' });
		List<ValidListItem> list = new List<ValidListItem>();
		string[] array2 = array;
		foreach (string value in array2)
		{
			list.Add(new ValidListItem
			{
				Value = value
			});
		}
		return list.ToArray();
	}

	private Component CreateOkCancelButtons(List<Component> pageComponents)
	{
		List<Component> list = new List<Component>();
		Component component = configuratorArgs.LayoutHelper.CreateButtonControl("Ok", "btnOkSmartString");
		((ErpButton)component.Model).Size = "ep-btn-size-medium";
		list.Add(component);
		Component component2 = configuratorArgs.LayoutHelper.CreateButtonControl("Cancel", "btnCancelSmartString");
		((ErpButton)component2.Model).Size = "ep-btn-size-medium";
		list.Add(component2);
		Component component3 = configuratorArgs.LayoutHelper.CreateRow();
		foreach (Component item2 in list)
		{
			Component component4 = configuratorArgs.LayoutHelper.CreateColumn("20%", component3.Id);
			component4.Components.Add(item2);
			component3.Components.Add(component4);
		}
		for (int i = 0; i < 2; i++)
		{
			Component item = configuratorArgs.LayoutHelper.CreateColumn("20%", component3.Id);
			component3.Components.Add(item);
		}
		return component3;
	}

	private SlidingPanelComponent CreateSlidingPanelButtons(bool showButtons = true)
	{
		List<SliderButton> list = new List<SliderButton>();
		if (showButtons)
		{
			SliderButton sliderButton = configuratorArgs.ConfiguratorLayoutHelper.CreateSliderButtonControl("Cancel", "btnCancelConfiguration");
			sliderButton.DialogResult = "Cancel";
			list.Add(sliderButton);
		}
		return new SlidingPanelComponent
		{
			ShowButtons = showButtons,
			Buttons = list
		};
	}
}
