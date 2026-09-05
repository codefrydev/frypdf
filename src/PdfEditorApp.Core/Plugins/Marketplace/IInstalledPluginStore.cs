using System;
using System.Collections.Generic;

namespace PdfEditorApp.Core.Plugins.Marketplace;

/// <summary>
/// Contract for persisting and retrieving installed plugin records and history across application sessions.
/// </summary>
public interface IInstalledPluginStore
{
    IReadOnlyList<InstalledPluginRecord> GetAll();
    InstalledPluginRecord? Get(string pluginId);
    bool IsInstalled(string pluginId);
    void AddOrUpdate(InstalledPluginRecord record);
    bool Remove(string pluginId);
    void Save();
}
