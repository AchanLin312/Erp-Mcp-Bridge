using Epicor.MetaFX.Core.Configurator;
using Epicor.MetaFX.Core.Configurator.Generation.Utils;
using Epicor.MetaFX.Core.Contracts;
using Epicor.MetaFX.Core.Helper;
using Epicor.MetaFX.Core.SchemaUpgrade;
using Epicor.MetaFX.Core.TemplateManager;

namespace Epicor.MetaFX.Core.Models.Configurator;

public class ConfiguratorArgs
{
	public string ImportPath { get; set; }

	public HelperMethods HelperMethods { get; set; }

	public Epicor.MetaFX.Core.SchemaUpgrade.SchemaUpgrade SchemaUpgrade { get; set; }

	public string OutputPath { get; set; }

	public Epicor.MetaFX.Core.TemplateManager.TemplateManager TemplateManager { get; set; }

	public ILogger Logger { get; set; }

	public ConfiguratorLayoutHelper ConfiguratorLayoutHelper { get; set; }

	public LayoutHelper LayoutHelper { get; set; }

	public string RootPath { get; set; }

	public Configuration Configuration { get; set; }

	public string ConfiguratorId { get; set; }

	public PcPage LayoutPage { get; set; }

	public ConfiguratorGeneratorHelper ConfiguratorHelper { get; set; }

	public CSharpExpressionParser ExpressionParser { get; set; }

	public IConfiguratorService ConfiguratorService { get; set; }

	public ExtendedConfiguration ExtendedConfiguration { get; set; }

	public DynamicAttrClass DynamicAttrClass { get; set; }

	public string LayoutType { get; set; }
}
