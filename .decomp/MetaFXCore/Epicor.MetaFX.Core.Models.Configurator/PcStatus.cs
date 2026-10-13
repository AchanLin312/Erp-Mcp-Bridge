using System;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class PcStatus
{
	public string Company { get; set; }

	public string PartNum { get; set; }

	public string RevisionNum { get; set; }

	public bool Approved { get; set; }

	public DateTimeOffset? ApprovedDate { get; set; }

	public string ApprovedBy { get; set; }

	public string StringStyle { get; set; }

	public string Separator { get; set; }

	public string NumberFormat { get; set; }

	public long StartNumber { get; set; }

	public bool CrtCustPart { get; set; }

	public bool PrefacePart { get; set; }

	public long ConfigVersion { get; set; }

	public string QuotePCode { get; set; }

	public string OrderPCode { get; set; }

	public bool OrdQuoCom { get; set; }

	public bool JobPickCom { get; set; }

	public bool ShipCom { get; set; }

	public bool InvCom { get; set; }

	public bool CreatePart { get; set; }

	public string CrtPartUsing { get; set; }

	public bool InQuoting { get; set; }

	public bool AutoCrtPart { get; set; }

	public bool NotUnique { get; set; }

	public bool InOrderEntry { get; set; }

	public bool InJobEntry { get; set; }

	public bool CreateBom { get; set; }

	public bool ZeroCost { get; set; }

	public bool CrtPartDesc { get; set; }

	public string FileName { get; set; }

	public bool SingleLevelConf { get; set; }

	public bool SaveInputValues { get; set; }

	public bool SetPartNumOnly { get; set; }

	public string Comments { get; set; }

	public bool AprvRev { get; set; }

	public bool EntprsConf { get; set; }

	public bool Synchronize { get; set; }

	public bool DispConfSummary { get; set; }

	public string PartComments { get; set; }

	public bool CompPricing { get; set; }

	public bool ExtConfig { get; set; }

	public string ExtMfgCompId { get; set; }

	public bool TestPlan { get; set; }

	public bool MarkMtlGlb { get; set; }

	public bool SiValuesUseQt { get; set; }

	public bool AllowSmartString { get; set; }

	public bool InDemandEntry { get; set; }

	public string DemandPCode { get; set; }

	public bool AllAltMethods { get; set; }

	public bool AllowReconPo { get; set; }

	public bool CompPricingJobMethod { get; set; }

	public bool CreateRev { get; set; }

	public string DefaultEco { get; set; }

	public bool GenerateMethod { get; set; }

	public bool PromptForConfig { get; set; }

	public bool PromptForCheckout { get; set; }

	public bool RemoveLink { get; set; }

	public bool SiValuesUseGenMethod { get; set; }

	public bool UseSavedLayout { get; set; }

	public string Description { get; set; }

	public string ConfigType { get; set; }

	public string ConfigId { get; set; }

	public bool RegenConfig { get; set; }

	public bool CreateNewConfigId { get; set; }

	public bool UseTemplatePartDefaults { get; set; }

	public string DefaultPartNum { get; set; }

	public string DefaultRevisionNum { get; set; }

	public long SysRevId { get; set; }

	public string SysRowId { get; set; }

	public string AttrClassId { get; set; }

	public bool RecycleJobs { get; set; }

	public object ClientDeployedToEwcDate { get; set; }

	public bool EwcClientSyncRequired { get; set; }

	public bool ShrinkFieldProperties { get; set; }

	public string AvailSmartStringInputs { get; set; }

	public string EcoGroup { get; set; }

	public bool HasInputs { get; set; }

	public bool IsValidPwd { get; set; }

	public bool ResetPcInputsAttributes { get; set; }

	public bool RevApproved { get; set; }

	public string SampleSmartString { get; set; }

	public string SelectedSmartStringInputs { get; set; }

	public bool UpdateCreatePartTarget { get; set; }

	public string AuditDesc { get; set; }

	public bool CanUpdate { get; set; }

	public long BitFlag { get; set; }

	public string PartNumTypeCode { get; set; }

	public bool PartNumTrackSerialNum { get; set; }

	public string PartNumPricePerCode { get; set; }

	public bool PartNumTrackDimension { get; set; }

	public string PartNumIum { get; set; }

	public string PartNumSalesUm { get; set; }

	public bool PartNumTrackLots { get; set; }

	public string PartNumPartDescription { get; set; }

	public long PartNumSellingFactor { get; set; }

	public string RowMod { get; set; }
}
