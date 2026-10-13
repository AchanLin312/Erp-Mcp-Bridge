using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ServiceModel;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Applications;
using Epicor.MetaFX.Core.Models.BAQReport;
using Epicor.MetaFX.Core.Models.Configurator;
using Epicor.MetaFX.Core.Models.Events;
using Epicor.MetaFX.Core.Models.Layers;
using Ice.Common;
using Ice.Hosting;
using Ice.Lib.MetaFX;

namespace Ice.Contracts;

/// <summary>
/// </summary>
[GeneratedCode("ServiceGenerator", "4.3.200.0")]
[ServiceContract("Ice:Lib:MetaFX")]
[ServiceContract(Namespace = "Ice:Lib:MetaFX")]
public interface MetaFXSvcContract : IDisposable
{
	/// <summary>
	/// View JSON layout and other meta data information
	/// </summary>
	/// <response code="200">View's JSON layout and other information is returned</response>
	/// <response code="500">Oops! Can't get view's meta data right now</response>
	/// <returns>View's JSON Metadata</returns>
	[OperationContract(Name = "GetApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetApp(EpMetaFxRequest request);

	/// <summary>
	/// View differences between two application meta data
	/// </summary>
	/// <response code="200">View's both apps with differences are returned</response>
	/// <response code="500">Oops! Can't get view's meta data right now</response>
	/// <returns>JSON Metadata for both app with differences in it</returns>
	[OperationContract(Name = "GetAppDifference")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetAppDifference(Epicor.MetaFX.Core.Models.EpMetaFxDiffRequest request);

	/// <summary>
	/// Get all views. Used in Menu maintenance
	/// </summary>
	/// <response code="200">Returns all views in array</response>
	/// <response code="500">Oops! Can't get view's metadata right now</response>
	/// <returns>All views</returns>
	[OperationContract(Name = "GetViews")]
	[FaultContract(typeof(EpicorFaultDetail))]
	string[] GetViews();

	/// <summary>
	/// Search dialog request and other metadata information
	/// </summary>
	/// <param name="request">EpMetaFxSearchRequest</param>
	[OperationContract(Name = "GetSearch")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetSearch(Ice.Lib.MetaFX.EpMetaFxSearchRequest request);

	/// <summary>
	/// Get default search folder and subfolder name for specified table
	/// </summary>
	/// <param name="request">Request parameters for finding default search</param>
	[OperationContract(Name = "GetDefaultSearch")]
	[FaultContract(typeof(EpicorFaultDetail))]
	EpMetaFxDefaultSearchResponse GetDefaultSearch(EpMetaFxDefaultSearchRequest request);

	/// <summary>
	/// Check for LIKE property folder
	/// </summary>
	/// <param name="request">EpMetaFxSearchRequest</param>
	[OperationContract(Name = "GetSearchForm")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetSearchForm(Ice.Lib.MetaFX.EpMetaFxSearchRequest request);

	/// <summary>
	/// Get Peek metadata information
	/// </summary>
	/// <param name="request"></param>
	/// <returns></returns>
	[OperationContract(Name = "GetPeek")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetPeek(Ice.Lib.MetaFX.EpMetaFxPeekRequest request);

	/// <summary>
	/// Gets folders and sub folders from peek folder.
	/// </summary>
	/// <returns></returns>
	[OperationContract(Name = "GetPeeks")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetPeeks();

	/// <summary>
	/// Gets the pre-defined templates like report/process etc
	/// </summary>
	/// <param name="templateType">EpMetaFxViewRequest</param>
	[OperationContract(Name = "GetTemplateView")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetTemplateView(EpMetaFxTemplateRequest templateType);

	/// <summary>
	/// Gets JSON files extended properties from the comobs
	/// </summary>
	/// <param name="request">EpMetaFxComboRequest</param>
	[OperationContract(Name = "GetCombosData")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetCombosData(Ice.Lib.MetaFX.EpMetaFxComboRequest request);

	/// <summary>
	/// Gets folders and sub folders names from combo shared folder
	/// </summary>
	[OperationContract(Name = "GetCombos")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetCombos();

	/// <summary>
	/// Upgrades the view
	/// </summary>
	/// <param name="request">EpMetaFxUpgradeSchemaRequest</param>
	[OperationContract(Name = "Upgrade")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object Upgrade(EpMetaFxUpgradeSchemaRequest request);

	/// <summary>
	/// Upgrade layer
	/// </summary>
	/// <param name="request">MetaFxUpgradeLayerRequest</param>
	/// <returns>TRUE if upgrade is successful</returns>
	[OperationContract(Name = "UpgradeLayer")]
	[FaultContract(typeof(EpicorFaultDetail))]
	bool UpgradeLayer(MetaFxUpgradeLayerRequest request);

	/// <summary>
	/// Executes custom data fix routines on customization layers for a given product version. Not supported for Base layers.
	/// This is different from the upgrade because the fix up uses the original version of the base from which the layer was created.
	/// </summary>
	[OperationContract(Name = "RunDataFixForLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void RunDataFixForLayers(EpMetaFXLayerFixRequest request);

	/// <summary>
	/// Saves the information.
	/// - In case of customization it saves as draft
	/// - In case of layers, it saves in DB as WIP
	/// </summary>
	/// <param name="request">EpMetaFxSaveRequest</param>
	[OperationContract(Name = "SaveApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void SaveApp(EpMetaFxSaveRequestEx request);

	/// <summary>
	/// publish the information.
	/// - In case of customization it replaces the actual file
	/// - In case of layers, it saves in DB
	/// </summary>
	/// <param name="request">EpMetaFxSaveRequest</param>
	[OperationContract(Name = "PublishApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void PublishApp(EpMetaFxSaveRequestEx request);

	/// <summary>
	/// Publishes all the layers in the list. Parent layers are not published automatically.
	/// </summary>
	/// <param name="layersToPublish">Layers to publish</param>
	/// <param name="validateBeforePublish">Validate each layer before publish (not implemented currently)</param>
	/// <returns></returns>
	[OperationContract(Name = "BulkPublishLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<EpMetaFxLayerForApplication> BulkPublishLayers(List<EpMetaFxLayerForApplication> layersToPublish, bool validateBeforePublish);

	/// <summary>
	/// Delete layers and applications in bulk.
	/// - SDK users can delete all applications and layers.
	/// - Non SDK users can delete applications if they can customize applications or if application does not have any layers.
	/// </summary>
	/// <param name="layersToDelete">Layers to Delete</param>
	/// <returns></returns>
	[OperationContract(Name = "BulkDeleteLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<EpMetaFxLayerForApplication> BulkDeleteLayers(List<EpMetaFxLayerForApplication> layersToDelete);

	/// <summary>
	/// Deletes cache folder
	/// </summary>
	/// <param name="request">EpMetaFxCacheInvalidateRequest</param>
	[OperationContract(Name = "InvalidateCache")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void InvalidateCache(Ice.Lib.MetaFX.EpMetaFxCacheInvalidateRequest request);

	/// <summary>
	/// returns all the folder names except common from root path(.\MetaUI\Shared\ViewTypes).
	/// </summary>
	/// <returns></returns>
	[OperationContract(Name = "GetViewTypes")]
	[FaultContract(typeof(EpicorFaultDetail))]
	string[] GetViewTypes();

	/// <summary>
	/// Gets the layer.
	/// </summary>
	/// <response code="200">Get the layer.</response>
	/// <response code="500">Internal server error.</response>
	/// <param name="request">Request to get layer.</param>
	[OperationContract(Name = "GetLayer")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetLayer(EpMetaFxLayerRequest request);

	/// <summary>
	/// Check if layer exists.
	/// </summary>
	/// <response code="200">Get layer exists status.</response>
	/// <response code="500">Internal server error.</response>
	/// <param name="request">Check if layer exists.</param>
	[OperationContract(Name = "LayerExists")]
	[FaultContract(typeof(EpicorFaultDetail))]
	bool LayerExists(EpMetaFxLayerRequest request);

	/// <summary>
	/// Get the layers.
	/// </summary>
	/// <response code="200">Get the layers.</response>
	/// <response code="500">Internal server error.</response>
	/// <param name="request">Request to get layers.</param>
	[OperationContract(Name = "GetLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetLayers(Ice.Lib.MetaFX.EpMetaFxGetLayersRequest request);

	/// <summary>
	/// Deletes the layer.
	/// </summary>
	/// <response code="200">Deleted the layer.</response>
	/// <response code="500">Internal server error.</response>
	/// <param name="request">Request to delete layer.</param>
	[OperationContract(Name = "DeleteLayer")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void DeleteLayer(EpMetaFxLayerRequest request);

	/// <summary>
	/// Checks if the application exists.
	/// </summary>
	/// <param name="applicationFullName">Complete name of the application including the prefix. E.g. Ice.UIDbd.SampleDashboard</param>
	/// <returns>True if the application exists</returns>
	[OperationContract(Name = "ApplicationExists")]
	[FaultContract(typeof(EpicorFaultDetail))]
	bool ApplicationExists(string applicationFullName);

	/// <summary>
	/// Checks if the component exists.
	/// </summary>
	/// <param name="componentFullName">Complete name of the component including the prefix. E.g. Ice.UIDbd.SampleDashboard</param>
	/// <returns>True if the component exists</returns>
	[OperationContract(Name = "ComponentExists")]
	[FaultContract(typeof(EpicorFaultDetail))]
	bool ComponentExists(string componentFullName);

	/// <summary>
	/// Generates the view.
	/// </summary>
	/// <param name="request">Request to generate view.</param>
	/// <response code="200">Views are generated.</response>
	/// <response code="500">Internal server error.</response>
	[OperationContract(Name = "GenerateView")]
	[FaultContract(typeof(EpicorFaultDetail))]
	Ice.Lib.MetaFX.EpMetaFxResponse<ReportResponse> GenerateView(EpMetaFxGenerateViewRequest request);

	/// <summary>
	/// This method returns audit log for layers.
	/// </summary>
	/// <param name="request">request</param>
	/// <returns></returns>
	[OperationContract(Name = "GetAuditLogs")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<EpMetaFXAuditLog> GetAuditLogs(EpMetaFXAuditLogRequest request);

	/// <summary>
	/// Download event schema
	/// </summary>
	/// <param name="request">DownloadSchemaRequest</param>
	/// <returns></returns>
	[OperationContract(Name = "DownloadSchema")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object DownloadSchema(DownloadSchemaRequest request);

	/// <summary>
	/// This method gets the event designer.
	/// </summary>
	/// <param name="request">request</param>
	/// <returns></returns>
	[OperationContract(Name = "GetEventDesigner")]
	[FaultContract(typeof(EpicorFaultDetail))]
	EpMetaFxEventDesigner GetEventDesigner(EpMetaFxEventDesignerRequest request);

	/// <summary>
	/// Get Application Types.
	/// </summary>
	/// <param name="applicationType">applicationType</param>
	/// <returns></returns>
	[OperationContract(Name = "GetApplicationSubTypes")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<ApplicationSubType> GetApplicationSubTypes(string applicationType);

	/// <summary>
	/// Get Applications.
	/// </summary>
	/// <param name="request">request</param>
	/// <returns></returns>
	[OperationContract(Name = "GetApplications")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<Application> GetApplications(ApplicationRequest request);

	/// <summary>
	/// Get New Application.
	/// </summary>
	/// <param name="request">request</param>
	/// <returns></returns>
	[OperationContract(Name = "GetNewApplication")]
	[FaultContract(typeof(EpicorFaultDetail))]
	ViewMetadataResponse GetNewApplication(Application request);

	/// <summary>
	/// Export App
	/// </summary>
	[OperationContract(Name = "ExportApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	AppContent ExportApp(string viewId);

	/// <summary>
	/// Import App
	/// </summary>
	[OperationContract(Name = "ImportApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void ImportApp(AppContent request);

	/// <summary>
	/// Export Layers
	/// Export currently supported for Custom Apps and layers not for System Apps
	/// </summary>
	[OperationContract(Name = "ExportLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	string ExportLayers(List<EpMetaFxLayerForApplication> apps);

	/// <summary>
	/// Import Layers
	/// The method imports Custom Apps and layers. System Apps are not supported.
	/// </summary>
	[OperationContract(Name = "ImportLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void ImportLayers(ImportLayerRequest fileContent);

	/// <summary>
	/// Duplicate Applications
	/// </summary>
	[OperationContract(Name = "DuplicateApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void DuplicateApp(DuplicateRequest request);

	/// <summary>
	/// Delete Applicatons
	/// </summary>
	[OperationContract(Name = "DeleteApp")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void DeleteApp(string viewId);

	/// <summary>
	/// Check for SystemFlag.
	/// </summary>
	/// <param name="viewId">viewId</param>
	/// <returns>True if SystemFlag is set to true</returns>
	[OperationContract(Name = "GetSystemApplicationStatus")]
	[FaultContract(typeof(EpicorFaultDetail))]
	object GetSystemApplicationStatus(string viewId);

	/// <summary>
	/// Update the security code for custom app.
	/// </summary>
	/// <param name="viewId">viewId</param>
	/// <param name="securityCode">securityCode</param>
	/// <returns>returns true if the update was successful</returns>
	[OperationContract(Name = "SetSecurityCodeForApplication")]
	[FaultContract(typeof(EpicorFaultDetail))]
	bool SetSecurityCodeForApplication(string viewId, string securityCode);

	/// <summary>
	/// GetConversionLogs
	/// </summary>
	[OperationContract(Name = "GetConversionLogs")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<ConversionLogMessageDetail> GetConversionLogs(string viewId);

	/// <summary>
	/// Delete personalization layers in bulk.
	/// - SM and GSM users can delete all personalization layers.
	/// </summary>
	/// <param name="layersToDelete">Personalization Layers to Delete</param>
	/// <returns></returns>
	[OperationContract(Name = "DeletePersonalizationLayers")]
	[FaultContract(typeof(EpicorFaultDetail))]
	List<EpMetaFxLayerForApplication> DeletePersonalizationLayers(List<EpMetaFxLayerForApplication> layersToDelete);

	/// <summary>
	/// Restores the version into the draft.
	/// </summary>
	/// <param name="request">Request object</param>
	[OperationContract(Name = "RestoreVersion")]
	[FaultContract(typeof(EpicorFaultDetail))]
	void RestoreVersion(EpMetaFxRequest request);
}
