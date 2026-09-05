using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Templates;

/// <summary>
/// Legacy interface for templates, inheriting from the plugin-extensible <see cref="ITemplateDescriptor"/>.
/// </summary>
public interface ITemplateDefinition : ITemplateDescriptor
{
}
