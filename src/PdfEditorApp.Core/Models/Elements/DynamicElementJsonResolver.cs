using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using PdfEditorApp.Core.Plugins.Descriptors;

namespace PdfEditorApp.Core.Models.Elements;

/// <summary>
/// Configures System.Text.Json polymorphic serialization for <see cref="PdfElementBase"/>
/// by dynamically reflecting all registered canvas element types from <see cref="ICanvasElementRegistry"/>.
/// Enables 3rd-party plugins to contribute novel canvas element models without modifying core enums.
/// </summary>
public static class DynamicElementJsonResolver
{
    public static JsonSerializerOptions CreateOptions(Func<IReadOnlyList<CanvasElementDescriptor>>? descriptorsProvider = null, bool writeIndented = false)
    {
        var resolver = new DefaultJsonTypeInfoResolver();

        if (descriptorsProvider != null)
        {
            resolver.Modifiers.Add(typeInfo =>
            {
                if (typeInfo.Type == typeof(PdfElementBase))
                {
                    var descriptors = descriptorsProvider();
                    if (descriptors != null && typeInfo.PolymorphismOptions != null)
                    {
                        foreach (var desc in descriptors)
                        {
                            if (!typeInfo.PolymorphismOptions.DerivedTypes.Any(d => d.DerivedType == desc.ModelType))
                            {
                                typeInfo.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(desc.ModelType, desc.ElementTypeId));
                            }
                        }
                    }
                }
            });
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            TypeInfoResolver = resolver,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
