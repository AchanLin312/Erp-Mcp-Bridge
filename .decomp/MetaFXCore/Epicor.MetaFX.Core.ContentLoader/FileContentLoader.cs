using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.Configurator;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;

namespace Epicor.MetaFX.Core.ContentLoader;

public class FileContentLoader : IContentLoader
{
	public bool DirectoryExists(string path)
	{
		return Directory.Exists(path);
	}

	public bool Exists(string path)
	{
		return File.Exists(path);
	}

	public string[] GetFiles(string path)
	{
		return Directory.GetFiles(path);
	}

	public string ReadAllText(string path, int uxAppVersion = 0)
	{
		return File.ReadAllText(path);
	}

	public void WriteAllText(string path, string contents)
	{
		File.WriteAllText(path, contents);
	}

	public void AppendAllText(string path, string contents)
	{
		File.AppendAllText(path, contents);
	}

	public void EmptyDirectory(string directoryPath)
	{
		DirectoryInfo directoryInfo = Directory.CreateDirectory(directoryPath);
		foreach (FileInfo item in directoryInfo.EnumerateFiles())
		{
			item.Delete();
		}
		foreach (DirectoryInfo item2 in directoryInfo.EnumerateDirectories())
		{
			EmptyDirectory(item2.FullName);
		}
	}

	public void Delete(string path)
	{
		File.Delete(path);
	}

	public string[] GetDirectories(string path, [Optional] Action<string, string, DateTime?> _onDirectoryRetrieved)
	{
		return Directory.GetDirectories(path);
	}

	public string[] GetDirectories(string path, string startsWith, [Optional] Action<string, string, DateTime?> _onDirectoryRetrieved)
	{
		return Directory.GetDirectories(path, startsWith + "*");
	}

	public AppContent ExportApp(string viewId, string company, string metaUI_Path)
	{
		Dictionary<string, string> files = new Dictionary<string, string>();
		string directory = Path.Combine(metaUI_Path, viewId);
		ReadFiles(directory, string.Empty, ref files, topLevel: true);
		if (viewId.StartsWith("Erp.UICfg.", StringComparison.OrdinalIgnoreCase))
		{
			viewId = new ConfiguratorHelper().GetConfigId(viewId, company);
		}
		return new AppContent
		{
			ViewId = viewId,
			Files = files
		};
	}

	private void ReadFiles(string directory, string relativePath, ref Dictionary<string, string> files, bool topLevel = false)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(directory);
		FileInfo[] files2 = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files2)
		{
			string empty = string.Empty;
			empty = ((!string.IsNullOrEmpty(relativePath)) ? Path.Combine(relativePath, fileInfo.Name) : fileInfo.Name);
			files.Add(empty, File.ReadAllText(fileInfo.FullName));
		}
		foreach (DirectoryInfo item in from d in directoryInfo.GetDirectories()
			where topLevel && !d.Name.EndsWith("cache")
			select d)
		{
			string empty2 = string.Empty;
			ReadFiles(relativePath: (!string.IsNullOrEmpty(relativePath)) ? Path.Combine(relativePath, item.Name) : item.Name, directory: item.FullName, files: ref files);
		}
	}

	public void ImportApp(AppContent request, string metaUI_Path, bool overwrite = true)
	{
		string text = Path.Combine(metaUI_Path, request.ViewId);
		if (Directory.Exists(text))
		{
			if (!overwrite)
			{
				return;
			}
			EmptyDirectory(text);
		}
		foreach (KeyValuePair<string, string> file in request.Files)
		{
			string text2 = Path.Combine(text, file.Key);
			FileInfo fileInfo = new FileInfo(text2);
			if (!Directory.Exists(text2))
			{
				fileInfo.Directory.Create();
			}
			if (request.ViewId.StartsWith("Erp.UICfg.", StringComparison.OrdinalIgnoreCase) && file.Key.Equals("patch.jsonc", StringComparison.OrdinalIgnoreCase))
			{
				ConfiguratorHelper configuratorHelper = new ConfiguratorHelper();
				File.WriteAllText(fileInfo.FullName, configuratorHelper.UpdateConfigApp(file, request.ViewId, request.CompanyId));
			}
			else
			{
				File.WriteAllText(fileInfo.FullName, file.Value);
			}
		}
	}

	public void DeleteApp(string viewId, string metaUI_Path)
	{
		string path = Path.Combine(metaUI_Path, viewId);
		if (Directory.Exists(path))
		{
			Directory.Delete(path, recursive: true);
		}
	}

	public void CreateDirectory(string path)
	{
		Directory.CreateDirectory(path);
	}

	public bool UseDbNotFileSystem()
	{
		return false;
	}

	public void CreateVersion(EpMetaFxLayerForApplication baseLayer)
	{
	}

	public void DiscardDraft(EpMetaFxRequest baseLayer, string rootPath)
	{
		string path = Path.Combine(rootPath, baseLayer.id);
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string item in from file in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
			where Path.GetFileName(file).IndexOf(".draft.jsonc", StringComparison.OrdinalIgnoreCase) >= 0
			select file)
		{
			if (Exists(item))
			{
				Delete(item);
			}
		}
	}
}
