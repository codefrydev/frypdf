using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PdfEditorApp.Models;

public class WorkflowStepDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public PdfToolId ToolId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string StepDescription { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string ParametersJson { get; set; } = "{}";

    public T GetParameters<T>() where T : class, new()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ParametersJson)) return new T();
            return JsonSerializer.Deserialize<T>(ParametersJson) ?? new T();
        }
        catch
        {
            return new T();
        }
    }

    public void SetParameters<T>(T parameters)
    {
        ParametersJson = JsonSerializer.Serialize(parameters);
    }
}

public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Automated Document Pipeline";
    public string Description { get; set; } = "Chain multiple PDF operations together into a reusable automated pipeline";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string OutputDirectory { get; set; } = string.Empty;
    public string OutputFileNamePattern { get; set; } = "{filename}_pipeline.pdf";
    public List<WorkflowStepDefinition> Steps { get; set; } = new();

    public WorkflowDefinition Clone()
    {
        var clone = new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"{Name} (Copy)",
            Description = Description,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
            OutputDirectory = OutputDirectory,
            OutputFileNamePattern = OutputFileNamePattern
        };

        foreach (var s in Steps)
        {
            clone.Steps.Add(new WorkflowStepDefinition
            {
                Id = Guid.NewGuid().ToString("N"),
                ToolId = s.ToolId,
                StepName = s.StepName,
                StepDescription = s.StepDescription,
                IsEnabled = s.IsEnabled,
                ParametersJson = s.ParametersJson
            });
        }

        return clone;
    }
}

public record WorkflowProgressInfo
{
    public int CurrentStepIndex { get; init; }
    public int TotalSteps { get; init; }
    public string CurrentStepName { get; init; } = string.Empty;
    public string CurrentFileName { get; init; } = string.Empty;
    public double OverallProgressPercentage { get; init; }
    public string StatusMessage { get; init; } = string.Empty;
}
