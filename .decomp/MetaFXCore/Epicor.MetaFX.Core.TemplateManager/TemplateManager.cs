using System;
using System.IO;
using System.Linq;
using Epicor.MetaFX.Core.ContentLoader;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.Models;
using Newtonsoft.Json.Linq;

namespace Epicor.MetaFX.Core.TemplateManager;

public class TemplateManager
{
	private readonly HelperMethods helper;

	private readonly ILogger logger;

	private readonly IContentLoader contentLoader;

	private readonly string rootPath;

	public TemplateManager(string rootPath, ILogger logger, IContentLoader contentLoader)
	{
		this.rootPath = Path.Combine(rootPath, "shared", "templates");
		this.logger = logger;
		this.contentLoader = contentLoader;
		helper = new HelperMethods(this.logger, contentLoader);
	}

	public object GetLayout(EpMetaFxTemplateRequest request, Func<string> currentCultureName)
	{
		currentCultureName();
		if (request.TemplateType != null && request.TemplateType.ToLower() == "pagetemplate")
		{
			JArray jArray = new JArray();
			string[] files = Directory.GetFiles(Path.Combine(rootPath, request.ViewType, "pages"), "*.jsonc", SearchOption.TopDirectoryOnly);
			foreach (string filePath in files)
			{
				JObject jObject = LoadTemplateContent(request.ViewType, filePath);
				jArray.Add(jObject["layout"]);
			}
			return jArray;
		}
		return LoadTemplate(request.ViewType)["layout"];
	}

	public object GetTemplate(EpMetaFxTemplateRequest request, Func<string> currentCultureName)
	{
		currentCultureName();
		return LoadTemplate(request.ViewType);
	}

	public string[] GetViewTypes()
	{
		if (!contentLoader.DirectoryExists(rootPath))
		{
			logger.Debug("Cannot find the path " + rootPath);
			throw new ArgumentException("Cannot find the path for the view types.");
		}
		string[] array = (from d in contentLoader.GetDirectories(rootPath)
			where !d.ToLower().EndsWith("common") && !d.ToLower().EndsWith("component")
			select d).ToArray();
		string[] array2 = new string[array.Length];
		for (int num = 0; num < array.Length; num++)
		{
			array2[num] = new DirectoryInfo(array[num]).Name;
		}
		return array2;
	}

	public object LoadTypesDefaultContent(EpMetaFxTemplateRequest request, string type, Func<string> currentCultureName)
	{
		if (!Directory.Exists(rootPath))
		{
			logger.Debug("Cannot find the path " + rootPath);
			throw new ArgumentException("Cannot find the root path.");
		}
		return LoadDefaultContent(request.TemplateType, type);
	}

	public object GetCommonTemplate(EpMetaFxTemplateRequest request, Func<string> currentCultureName)
	{
		currentCultureName();
		return LoadCommonTemplate(request.ViewType);
	}

	private JObject LoadTemplate(string viewType)
	{
		string filePath = helper.GetFilePath(Path.Combine(rootPath, viewType), "layout");
		return LoadTemplateContent(viewType, filePath);
	}

	private JObject LoadDefaultContent(string templateType, string type)
	{
		logger.Debug("retrieving default content " + templateType);
		string text = templateType.ToUpper() switch
		{
			"TRACKERPAGE" => helper.GetFilePath(Path.Combine(rootPath, "Trackers"), type), 
			"APPSPAGE" => helper.GetFilePath(Path.Combine(rootPath, "Apps"), type), 
			"BAQREPORT" => helper.GetFilePath(Path.Combine(rootPath, "Report"), type), 
			_ => helper.GetFilePath(Path.Combine(rootPath, templateType), type), 
		};
		if (!contentLoader.Exists(text))
		{
			logger.Debug("Processing Error : No file found " + text);
			throw new FileNotFoundException("Processing Error : No file found '" + Path.GetFileName(text) + "'");
		}
		return JObject.Parse(contentLoader.ReadAllText(text));
	}

	private JObject LoadCommonTemplate(string viewType)
	{
		string filePath = helper.GetFilePath(Path.Combine(rootPath, "Common", viewType), "layout");
		return LoadTemplateContent(viewType, filePath);
	}

	private JObject LoadTemplateContent(string viewType, string filePath)
	{
		logger.Debug("retrieving default templates for " + viewType);
		if (!contentLoader.Exists(filePath))
		{
			logger.Debug("Processing Error : No file found " + filePath);
			throw new FileNotFoundException("Processing Error : No file found '" + Path.GetFileName(filePath) + "'");
		}
		JObject jObject = JObject.Parse(contentLoader.ReadAllText(filePath));
		if (viewType == "BAQReport")
		{
			return jObject;
		}
		FlattenLayout flattenLayout = new FlattenLayout();
		jObject["layout"] = flattenLayout.ConvertToHierarchicalStructure(jObject["layout"] as JObject);
		return jObject;
	}
}
