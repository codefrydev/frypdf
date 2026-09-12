## Summary
<!-- What does this PR do, and why? -->

## Related Issue
Closes #

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Refactor (no functional change)
- [ ] Documentation
- [ ] Plugin (new or updated `IFryPlugin`)
- [ ] Build / CI / tooling

## Checklist
- [ ] `dotnet build` succeeds with zero warnings (`TreatWarningsAsErrors=true`)
- [ ] `dotnet test` passes locally
- [ ] Added/updated unit tests in `tests/PdfEditorApp.Tests/` for new behavior
- [ ] New capabilities are implemented as plugins (`IFryPlugin` / `ToolPluginBase`),
      not hardcoded switch-cases — see [Plugin-Based Architecture](docs/PLUGIN_BASED_ARCHITECTURE.md)
- [ ] UI changes follow [Material Design 3 Expressive](docs/MATERIAL_DESIGN_3_EXPRESSIVE_GUIDELINES.md)
      (shape tokens, dynamic color brushes, no hardcoded hex)
- [ ] No PII, credentials, or real personal documents included (synthetic/dummy data only)
- [ ] Docs updated if behavior, architecture, or setup changed

## Screenshots / Recordings
<!-- For UI changes, before/after screenshots or a short screen recording help a lot -->
