using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Events;
using Epicor.MetaFX.Core.Models.Layers;
using Ice.Cloud;
using Ice.Contracts;
using Ice.Lib.MetaFX;
using Ice.Security;

namespace Ice.Proxy.Lib;

[DebuggerNonUserCode]
[GeneratedCode("ServiceGenerator", "4.3.200.0")]
public class MetaFXImpl : ProxyBase<MetaFXSvcContract>
{
	/// <summary>
	/// The relative path to the API.
	/// </summary>
	public new static readonly string UriPath = "Ice/Lib/MetaFX";

	public MetaFXImpl(Uri baseAddress, string userId, string password, AuthenticationMode authenticationMode)
		: base(baseAddress, "Ice.Lib.MetaFXSvc", userId, password, authenticationMode)
	{
	}

	public object GetApp(EpMetaFxRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetApp", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetAppDifference(Epicor.MetaFX.Core.Models.EpMetaFxDiffRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Epicor.MetaFX.Core.Models.EpMetaFxDiffRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetAppDifference", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public string[] GetViews()
	{
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(string[]), (object)null));
		Call("GetViews", ProxyValuesIn.Empty, proxyValuesOut, useSparseCopy: false);
		return (string[])proxyValuesOut.ReturnInfo.Value;
	}

	public object GetSearch(Ice.Lib.MetaFX.EpMetaFxSearchRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxSearchRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetSearch", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public EpMetaFxDefaultSearchResponse GetDefaultSearch(EpMetaFxDefaultSearchRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxDefaultSearchRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(EpMetaFxDefaultSearchResponse), (object)null));
		Call("GetDefaultSearch", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (EpMetaFxDefaultSearchResponse)proxyValuesOut.ReturnInfo.Value;
	}

	public object GetSearchForm(Ice.Lib.MetaFX.EpMetaFxSearchRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxSearchRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetSearchForm", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetPeek(Ice.Lib.MetaFX.EpMetaFxPeekRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxPeekRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetPeek", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetPeeks()
	{
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetPeeks", ProxyValuesIn.Empty, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetTemplateView(EpMetaFxTemplateRequest templateType)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("templateType", typeof(EpMetaFxTemplateRequest), templateType));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetTemplateView", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetCombosData(Ice.Lib.MetaFX.EpMetaFxComboRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxComboRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetCombosData", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object GetCombos()
	{
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetCombos", ProxyValuesIn.Empty, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public object Upgrade(EpMetaFxUpgradeSchemaRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxUpgradeSchemaRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("Upgrade", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public bool UpgradeLayer(MetaFxUpgradeLayerRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(MetaFxUpgradeLayerRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo(null, typeof(bool), false));
		Call("UpgradeLayer", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (bool)proxyValuesOut.ReturnInfo.Value;
	}

	public void RunDataFixForLayers(EpMetaFXLayerFixRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFXLayerFixRequest), request));
		Call("RunDataFixForLayers", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public void SaveApp(EpMetaFxSaveRequestEx request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxSaveRequestEx), request));
		Call("SaveApp", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public void PublishApp(EpMetaFxSaveRequestEx request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxSaveRequestEx), request));
		Call("PublishApp", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, bool validateBeforePublish)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("layersToPublish", typeof(List<EpMetaFxLayerForApplication>), layersToPublish), new ProxyValueInInfo("validateBeforePublish", typeof(bool), validateBeforePublish));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<EpMetaFxLayerForApplication>), (object)null));
		Call("BulkPublishLayers", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<EpMetaFxLayerForApplication>)proxyValuesOut.ReturnInfo.Value;
	}

	public List<EpMetaFxLayerForApplication> BulkDeleteLayers(List<EpMetaFxLayerForApplication> layersToDelete)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("layersToDelete", typeof(List<EpMetaFxLayerForApplication>), layersToDelete));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<EpMetaFxLayerForApplication>), (object)null));
		Call("BulkDeleteLayers", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<EpMetaFxLayerForApplication>)proxyValuesOut.ReturnInfo.Value;
	}

	public void InvalidateCache(Ice.Lib.MetaFX.EpMetaFxCacheInvalidateRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxCacheInvalidateRequest), request));
		Call("InvalidateCache", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public string[] GetViewTypes()
	{
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(string[]), (object)null));
		Call("GetViewTypes", ProxyValuesIn.Empty, proxyValuesOut, useSparseCopy: false);
		return (string[])proxyValuesOut.ReturnInfo.Value;
	}

	public object GetLayer(EpMetaFxLayerRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxLayerRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetLayer", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public bool LayerExists(EpMetaFxLayerRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxLayerRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo(null, typeof(bool), false));
		Call("LayerExists", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (bool)proxyValuesOut.ReturnInfo.Value;
	}

	public object GetLayers(Ice.Lib.MetaFX.EpMetaFxGetLayersRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Ice.Lib.MetaFX.EpMetaFxGetLayersRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetLayers", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public void DeleteLayer(EpMetaFxLayerRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxLayerRequest), request));
		Call("DeleteLayer", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public bool ApplicationExists(string applicationFullName)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("applicationFullName", typeof(string), applicationFullName));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo(null, typeof(bool), false));
		Call("ApplicationExists", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (bool)proxyValuesOut.ReturnInfo.Value;
	}

	public bool ComponentExists(string componentFullName)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("componentFullName", typeof(string), componentFullName));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo(null, typeof(bool), false));
		Call("ComponentExists", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (bool)proxyValuesOut.ReturnInfo.Value;
	}

	public Ice.Lib.MetaFX.EpMetaFxResponse<ReportResponse> GenerateView(EpMetaFxGenerateViewRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxGenerateViewRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(Ice.Lib.MetaFX.EpMetaFxResponse<ReportResponse>), (object)null));
		Call("GenerateView", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (Ice.Lib.MetaFX.EpMetaFxResponse<ReportResponse>)proxyValuesOut.ReturnInfo.Value;
	}

	public List<EpMetaFXAuditLog> GetAuditLogs(EpMetaFXAuditLogRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFXAuditLogRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<EpMetaFXAuditLog>), (object)null));
		Call("GetAuditLogs", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<EpMetaFXAuditLog>)proxyValuesOut.ReturnInfo.Value;
	}

	public object DownloadSchema(DownloadSchemaRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(DownloadSchemaRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("DownloadSchema", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public EpMetaFxEventDesigner GetEventDesigner(EpMetaFxEventDesignerRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxEventDesignerRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(EpMetaFxEventDesigner), (object)null));
		Call("GetEventDesigner", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (EpMetaFxEventDesigner)proxyValuesOut.ReturnInfo.Value;
	}

	public List<ApplicationSubType> GetApplicationSubTypes(string applicationType)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("applicationType", typeof(string), applicationType));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<ApplicationSubType>), (object)null));
		Call("GetApplicationSubTypes", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<ApplicationSubType>)proxyValuesOut.ReturnInfo.Value;
	}

	public List<Application> GetApplications(ApplicationRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(ApplicationRequest), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<Application>), (object)null));
		Call("GetApplications", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<Application>)proxyValuesOut.ReturnInfo.Value;
	}

	public ViewMetadataResponse GetNewApplication(Application request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(Application), request));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(ViewMetadataResponse), (object)null));
		Call("GetNewApplication", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (ViewMetadataResponse)proxyValuesOut.ReturnInfo.Value;
	}

	public AppContent ExportApp(string viewId)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("viewId", typeof(string), viewId));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(AppContent), (object)null));
		Call("ExportApp", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (AppContent)proxyValuesOut.ReturnInfo.Value;
	}

	public void ImportApp(AppContent request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(AppContent), request));
		Call("ImportApp", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public string ExportLayers(List<EpMetaFxLayerForApplication> apps)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("apps", typeof(List<EpMetaFxLayerForApplication>), apps));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(string), (object)null));
		Call("ExportLayers", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (string)proxyValuesOut.ReturnInfo.Value;
	}

	public void ImportLayers(ImportLayerRequest fileContent)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("fileContent", typeof(ImportLayerRequest), fileContent));
		Call("ImportLayers", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public void DuplicateApp(DuplicateRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(DuplicateRequest), request));
		Call("DuplicateApp", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public void DeleteApp(string viewId)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("viewId", typeof(string), viewId));
		Call("DeleteApp", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}

	public object GetSystemApplicationStatus(string viewId)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("viewId", typeof(string), viewId));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(object), (object)null));
		Call("GetSystemApplicationStatus", valuesIn, proxyValuesOut, useSparseCopy: false);
		return proxyValuesOut.ReturnInfo.Value;
	}

	public bool SetSecurityCodeForApplication(string viewId, string securityCode)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("viewId", typeof(string), viewId), new ProxyValueInInfo("securityCode", typeof(string), securityCode));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo(null, typeof(bool), false));
		Call("SetSecurityCodeForApplication", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (bool)proxyValuesOut.ReturnInfo.Value;
	}

	public List<ConversionLogMessageDetail> GetConversionLogs(string viewId)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("viewId", typeof(string), viewId));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<ConversionLogMessageDetail>), (object)null));
		Call("GetConversionLogs", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<ConversionLogMessageDetail>)proxyValuesOut.ReturnInfo.Value;
	}

	public List<EpMetaFxLayerForApplication> DeletePersonalizationLayers(List<EpMetaFxLayerForApplication> layersToDelete)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("layersToDelete", typeof(List<EpMetaFxLayerForApplication>), layersToDelete));
		ProxyValuesOut proxyValuesOut = new ProxyValuesOut(new ProxyValueOutInfo((string)null, typeof(List<EpMetaFxLayerForApplication>), (object)null));
		Call("DeletePersonalizationLayers", valuesIn, proxyValuesOut, useSparseCopy: false);
		return (List<EpMetaFxLayerForApplication>)proxyValuesOut.ReturnInfo.Value;
	}

	public void RestoreVersion(EpMetaFxRequest request)
	{
		ProxyValuesIn valuesIn = new ProxyValuesIn(new ProxyValueInInfo("request", typeof(EpMetaFxRequest), request));
		Call("RestoreVersion", valuesIn, ProxyValuesOut.Empty, useSparseCopy: false);
	}
}
