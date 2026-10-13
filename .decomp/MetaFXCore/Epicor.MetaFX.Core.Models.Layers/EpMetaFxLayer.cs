using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.Models.Layers;

public class EpMetaFxLayer : EpMetaFxLayerCommon, IValidatableObject
{
	[Required]
	public override string ViewId { get; set; }

	[StringLength(60, ErrorMessage = "Layer Description cannot be longer than 60 characters.")]
	public string LayerDescription { get; set; }

	public string[] ParentLayers { get; set; }

	public string CommentText { get; set; }

	public bool WIP { get; set; }

	[Required]
	public string UserName { get; set; }

	public bool IsNew { get; set; }

	[Required]
	public Dictionary<string, JToken> Content { get; set; }

	public DateTime? ChangedOn { get; set; }

	public bool? LayerUpdatedToPropDiffFormat { get; set; }

	public Dictionary<string, LayerProcessedInfo> ProcessedInfo { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		List<ValidationResult> list = new List<ValidationResult>();
		if (base.TypeCode != "KNTCCustLayer" && base.TypeCode != "KNTCPersLayer")
		{
			list.Add(new ValidationResult("TypeCode should be either KNTCCustLayer or KNTCPersLayer."));
		}
		if (base.TypeCode == "KNTCCustLayer" && string.IsNullOrWhiteSpace(base.LayerName))
		{
			list.Add(new ValidationResult("Layer Name should not be empty."));
		}
		return list;
	}
}
