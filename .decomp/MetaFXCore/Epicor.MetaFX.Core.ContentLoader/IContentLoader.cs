using System;
using System.Runtime.InteropServices;
using Epicor.MetaFX.Core.Models;
using Epicor.MetaFX.Core.Models.Layers;

namespace Epicor.MetaFX.Core.ContentLoader;

public interface IContentLoader
{
	bool Exists(string path);

	string ReadAllText(string path, int uxAppVersion = 0);

	void WriteAllText(string path, string contents);

	void AppendAllText(string path, string contents);

	bool DirectoryExists(string path);

	string[] GetFiles(string path);

	void EmptyDirectory(string directoryPath);

	void Delete(string path);

	string[] GetDirectories(string path, [Optional] Action<string, string, DateTime?> onDirectoryRetrieved);

	string[] GetDirectories(string path, string startsWith, [Optional] Action<string, string, DateTime?> onDirectoryRetrieved);

	AppContent ExportApp(string viewId, string company, string metaUI_Path);

	void ImportApp(AppContent request, string metaUI_Path, [Optional] bool overwrite);

	void DeleteApp(string viewId, string metaUI_Path);

	void CreateDirectory(string path);

	bool UseDbNotFileSystem();

	void CreateVersion(EpMetaFxLayerForApplication baseLayer);

	void DiscardDraft(EpMetaFxRequest request, string rootPath);
}
