using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.AppStudio.Metadata;
using Epicor.MetaFX.Core.AppStudio.Metadata.@abstract;
using Epicor.MetaFX.Core.Applications;
using Epicor.MetaFX.Core.Cache;
using Epicor.MetaFX.Core.Configurator.Metadata;
using Epicor.MetaFX.Core.Configurator.Wasm;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Layers;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Configurator;

public class ConfiguratorManager
{
	private readonly ILogger logger;

	private readonly IConfiguratorService configuratorService;

	private readonly IContentLoader contentLoader;

	private readonly ICacheService cacheService;

	private readonly string rootPath;

	private readonly Services services;

	private static readonly string[] FilesToIgnore = new string[2] { "smartstring", "configurationsummary" };

	private static readonly string[] PageEvents = new string[2] { "Loaded", "Leave" };

	private const string ReadOnlyExpression = "ReadOnlyExpression";

	private readonly LayoutHelper layoutHelper;

	private readonly ConfiguratorHelper configuratorHelper;

	private readonly IUserDefinedApplicationService userDefinedApplicationSvc;

	public ConfiguratorManager(string rootPath, ILogger logger, IConfiguratorService configuratorService, Services services, IContentLoader contentLoader, ICacheService cacheService, IUserDefinedApplicationService userDefinedApplicationService = null)
	{
		this.logger = logger;
		this.configuratorService = configuratorService;
		this.contentLoader = contentLoader;
		this.cacheService = cacheService;
		this.rootPath = rootPath;
		this.services = services;
		layoutHelper = new LayoutHelper(logger);
		configuratorHelper = new ConfiguratorHelper();
		userDefinedApplicationSvc = userDefinedApplicationService;
	}

	public ViewMetadataResponse LoadConfiguratorDetails(EpMetaFxRequest request, ViewMetadataResponse response)
	{
		WasmManager wasmManager = new WasmManager(rootPath, logger, configuratorService, contentLoader, null, userDefinedApplicationSvc);
		ApplicationSubType applicationSubType = new ApplicationManager(rootPath, logger, contentLoader, this.cacheService).GetApplicationSubTypes("Configurator").FirstOrDefault();
		response.IsLayerDisabled = applicationSubType?.IsLayerDisabled ?? true;
		ICacheService cacheService = this.cacheService;
		if (request.properties.mode == "AppStudio")
		{
			cacheService = new NoCacheService();
		}
		if (request.properties.includeWasm)
		{
			WasmResponse wasmResponse = cacheService.Get("wasm.json", () => (response.Events == null) ? null : wasmManager.CreateWasm(request, configuratorHelper.GetAllExecuteCharpEventActions(response.Events as JArray)));
			if (wasmResponse != null)
			{
				response.Wasm = wasmResponse.Wasm;
			}
		}
		if (request.properties.mode != "AppStudio")
		{
			return response;
		}
		JArray jArray = new JArray();
		foreach (JToken page in response.Pages)
		{
			if (!Enumerable.Contains(FilesToIgnore, page["name"].ToString().ToLowerInvariant()))
			{
				jArray.Add(page);
			}
		}
		response.Pages = jArray;
		Configuration configuration = configuratorService.GetConfiguration(configuratorHelper.GetConfigId(request.id, request.properties.company));
		response.ConfiguratorInfo = new ConfiguratorInfo
		{
			ConfigId = configuratorHelper.GetConfigId(request.id, request.properties.company),
			Approved = configuration.PcStatus[0].Approved,
			Description = configuration.PcStatus[0].Description,
			AuditDesc = configuration.PcStatus[0].AuditDesc,
			DispConfSummary = configuration.PcStatus[0].DispConfSummary,
			DynamicAttributeClassId = configuration.PcStatus[0].AttrClassId,
			AprvRev = configuration.PcStatus[0].AprvRev
		};
		foreach (PcStatusExpr item3 in configuration.PcStatusExpr)
		{
			StatusExpr item = new StatusExpr
			{
				TypeCode = item3.TypeCode,
				Expression = item3.Expression
			};
			response.ConfiguratorInfo.StatusExpr.Add(item);
		}
		foreach (PcExpr item4 in configuration.PcPageExpr)
		{
			PcPageExpr item2 = new PcPageExpr
			{
				TypeCode = item4.TypeCode,
				Expression = item4.Expression,
				PageName = GetPage(item4.PageSeq.GetValueOrDefault())
			};
			response.ConfiguratorInfo.PcPageExpr.Add(item2);
		}
		if (request.properties.mode == "AppStudio")
		{
			UDGetRowsRequest request2 = new UDGetRowsRequest
			{
				WhereClausePcFunctionDef = "ConfigID = '" + configuratorHelper.GetConfigId(request.id, request.properties.company) + "'"
			};
			ConfiguratorUDFunction uDFunction = configuratorService.GetUDFunction(request2);
			response.ConfiguratorInfo.ConfigUDFunc = uDFunction;
		}
		return response;
	}

	public void SaveConfigurator(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		UpdateConfiguratorResponse updateResponse = SaveConfiguratorInternal(request, currentCultureName, isPublish: false);
		GetMetaDatas(updateResponse).ForEach(delegate(BaseMetadataType designer)
		{
			designer.Save(request, currentCultureName, localizedStringsFunc);
		});
	}

	public void PublishConfigurator(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		UpdateConfiguratorResponse updateResponse = SaveConfiguratorInternal(request, currentCultureName, isPublish: true);
		GetMetaDatas(updateResponse).ForEach(delegate(BaseMetadataType designer)
		{
			designer.Publish(request, currentCultureName, localizedStringsFunc);
		});
	}

	public List<EpMetaFxLayerForApplication> BulkPublishConfigurators(List<EpMetaFxLayerForApplication> appsToPublish, Func<string> currentCultureName, Func<JObject, string, JObject> localizedStringsFunc)
	{
		EpMetaFxSaveRequestEx request;
		foreach (EpMetaFxLayerForApplication item in appsToPublish)
		{
			try
			{
				string configId = configuratorHelper.GetConfigId(item.Id, item.Company);
				if (contentLoader.DirectoryExists(Path.Combine(rootPath, item.Id)))
				{
					UpdateConfiguratorResponse updateResponse = configuratorService.ApproveConfiguratorBeforeBulkPublish(configId);
					List<BaseMetadataType> metaDatas = GetMetaDatas(updateResponse);
					request = new EpMetaFxSaveRequestEx
					{
						id = item.Id,
						viewType = "Configurator"
					};
					if (!metaDatas.Any((BaseMetadataType designer) => designer.HasActiveDraft(request.id)))
					{
						item.PublishStatus = StatusValue.Failed;
						item.PublishError = "Publish failed for base layer for Application " + item.Id + ". Error: Layer does not contain an active draft.";
						continue;
					}
					metaDatas.ForEach(delegate(BaseMetadataType designer)
					{
						designer.Publish(request, currentCultureName, localizedStringsFunc, inBulkPublish: true);
					});
					item.PublishStatus = StatusValue.Success;
					item.PublishError = null;
					item.IsPublished = true;
					item.HasDraftContent = false;
					item.LastUpdated = DateTime.UtcNow;
				}
				else
				{
					item.PublishStatus = StatusValue.Failed;
					item.PublishError = "Publish failed for base layer for Application " + item.Id + ". Error: Base layer not found.";
				}
			}
			catch (Exception ex)
			{
				item.PublishStatus = StatusValue.Failed;
				item.PublishError = ex.Message;
			}
		}
		return appsToPublish;
	}

	public List<EpMetaFXAuditLog> GetAuditLogs(EpMetaFXAuditLogRequest request)
	{
		return configuratorService.GetApproveLogs(configuratorHelper.GetConfigId(request.ViewId, request.Company));
	}

	private UpdateConfiguratorResponse SaveConfiguratorInternal(EpMetaFxSaveRequestEx request, Func<string> currentCultureName, bool isPublish)
	{
		CreateOrRemoveHerobar(request);
		Dictionary<string, JObject> pages = GetPages(request);
		UpdateConfiguratorInfo(request, pages);
		new ConfiguratorEventMetadata(rootPath, logger, services, contentLoader, null, configuratorService, cacheService);
		UpdateConfiguratorRequest updateConfiguratorRequest = new UpdateConfiguratorRequest
		{
			Request = request,
			Pages = pages
		};
		UpdateConfiguratorResponse updateConfiguratorResponse = null;
		if (!isPublish)
		{
			updateConfiguratorResponse = configuratorService.UpdateConfigurator(updateConfiguratorRequest);
		}
		else
		{
			Configuration configuration = configuratorService.GetConfiguration(request.configuratorInfo.ConfigId);
			if (!configuration.PcStatus[0].Approved)
			{
				updateConfiguratorResponse = configuratorService.UpdateConfigurator(updateConfiguratorRequest);
			}
			else
			{
				updateConfiguratorResponse = new UpdateConfiguratorResponse();
				updateConfiguratorResponse.Configuration = configuration;
				updateConfiguratorResponse.PreviuosConfiguration = configuration;
			}
		}
		if (!string.IsNullOrEmpty(updateConfiguratorResponse.Configuration.PcStatus[0].AttrClassId))
		{
			updateConfiguratorResponse.DynamicAttrClass = configuratorService.GetDynamicAttrClass(updateConfiguratorResponse.Configuration.PcStatus[0].AttrClassId);
		}
		UpdatePages(updateConfiguratorResponse, updateConfiguratorRequest);
		return updateConfiguratorResponse;
	}

	private Dictionary<string, JObject> GetPages(EpMetaFxSaveRequestEx request, bool heirarchicalStructure = false)
	{
		Dictionary<string, JObject> dictionary = new Dictionary<string, JObject> { 
		{
			request.layout["name"].ToString(),
			heirarchicalStructure ? request.layout : Flatten(request.layout)
		} };
		foreach (JToken page in request.pages)
		{
			if (page["viewType"].ToString() == "TabPage")
			{
				dictionary.Add(page["name"].ToString(), heirarchicalStructure ? (page as JObject) : Flatten(page as JObject));
			}
		}
		return dictionary;
	}

	private string GetPage(long pageSeq)
	{
		return "Page" + pageSeq;
	}

	private JObject Flatten(JObject obj)
	{
		return new FlattenLayout().ConvertToFlattenLayout(obj);
	}

	private JObject ConvertToHierarchicalStructure(JObject obj)
	{
		return new FlattenLayout().ConvertToHierarchicalStructure(obj);
	}

	private void UpdateConfiguratorInfo(EpMetaFxSaveRequestEx request, Dictionary<string, JObject> pages)
	{
		if (request.events == null)
		{
			return;
		}
		foreach (KeyValuePair<string, JObject> page in pages)
		{
			foreach (JToken @event in request.events)
			{
				string text = @event["id"].ToString();
				if (string.IsNullOrEmpty(text) || !Enumerable.Contains(text, '_'))
				{
					continue;
				}
				string[] array = @event["id"].ToString().Split(new char[1] { '_' });
				if (array.Length < 2)
				{
					continue;
				}
				string typeCode = array[0];
				if (Enumerable.Contains(PageEvents, typeCode))
				{
					PcPageExpr pcPageExpr = request.configuratorInfo.PcPageExpr.FirstOrDefault((PcPageExpr p) => p.TypeCode == typeCode && p.PageName == page.Key);
					if (pcPageExpr == null)
					{
						pcPageExpr = new PcPageExpr
						{
							PageName = page.Key,
							TypeCode = typeCode
						};
						request.configuratorInfo.PcPageExpr.Add(pcPageExpr);
					}
					if (!(@event["actions"] is JArray) || ((JArray)@event["actions"]).Count == 0 || @event["actions"][0]["value"] == null || @event["mode"]?.ToString() == "D")
					{
						pcPageExpr.Expression = string.Empty;
					}
					else
					{
						pcPageExpr.Expression = @event["actions"][0]["value"].ToString();
					}
				}
			}
		}
	}

	private void UpdatePages(UpdateConfiguratorResponse updateResponse, UpdateConfiguratorRequest updateRequest)
	{
		Dictionary<string, JObject> pages = updateRequest.Pages;
		foreach (PcInput control in updateResponse.Configuration.PcInputs)
		{
			JToken jToken = (pages[GetPage(control.PageSeq)]["components"] as JArray).FirstOrDefault((JToken comp) => comp["id"]?.ToString() == control.InputName);
			if (jToken == null || !(jToken["sourceTypeId"]?.ToString() == "erp-combo-box") || !(jToken["model"]["dynamicLists"] is JArray jArray))
			{
				continue;
			}
			JArray jArray2 = new JArray();
			foreach (JToken item in jArray)
			{
				if (item["listDataSource"] != null)
				{
					jArray2.Add(item);
				}
			}
			jToken["model"]["dynamicLists"] = jArray2;
		}
		foreach (KeyValuePair<string, JObject> page in pages)
		{
			JObject jObject = ConvertToHierarchicalStructure(page.Value);
			if (page.Key == updateRequest.Request.layout["name"].ToString())
			{
				updateRequest.Request.layout = jObject;
			}
			else if (updateRequest.Request.pages != null)
			{
				JToken jToken2 = updateRequest.Request.pages.FirstOrDefault((JToken p) => p["name"].ToString() == page.Key);
				if (jToken2 != null)
				{
					updateRequest.Request.pages.Remove(jToken2);
					updateRequest.Request.pages.Add(jObject);
				}
			}
		}
	}

	private void CreateOrRemoveHerobar(EpMetaFxSaveRequestEx request)
	{
		if (request.pages == null || !request.pages.Any() || !request.pages.Any((JToken pg) => pg["viewType"]?.ToString() == "TabPage"))
		{
			if (request.layout["heroBar"] != null)
			{
				request.layout.Remove("heroBar");
			}
		}
		else if (request.layout["heroBar"] == null)
		{
			var obj = new
			{
				items = new object[1]
				{
					new
					{
						type = 2,
						component = layoutHelper.CreateRowNavigatorControl("Page", Guid.NewGuid().ToString(), "[PcPage]")
					}
				}
			};
			request.layout["heroBar"] = layoutHelper.ConvertToJObject(obj);
		}
	}

	private List<BaseMetadataType> GetMetaDatas(UpdateConfiguratorResponse updateResponse)
	{
		return new List<BaseMetadataType>
		{
			new LayoutMetadata(rootPath, logger, contentLoader, cacheService),
			new ConfiguratorPageMetadata(rootPath, logger, contentLoader, cacheService),
			new ConfiguratorEventMetadata(rootPath, logger, services, contentLoader, updateResponse, configuratorService, cacheService),
			new ConfiguratorRowRuleMetadata(rootPath, logger, contentLoader, cacheService),
			new ToolMetadata(rootPath, logger, contentLoader, cacheService),
			new Epicor.MetaFX.Core.Configurator.Metadata.DataViewMetadata(rootPath, logger, contentLoader, cacheService)
		};
	}
}
