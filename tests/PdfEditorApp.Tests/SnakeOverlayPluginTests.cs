using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;
using PdfEditorApp.Plugins.Bundles;
using PdfEditorApp.Plugins.Snake;
using PdfEditorApp.Services.Overlays;
using PdfEditorApp.Services.Palette;
using PdfEditorApp.Services.Ribbon;
using PdfEditorApp.Services.StatusBar;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class SnakeOverlayPluginTests
{
    [Fact]
    public void OverlayRegistry_Registers_Shows_Toggles_And_Hides_Overlay()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new OverlayRegistry(services);

        bool registryChangedFired = false;
        registry.RegistryChanged += () => registryChangedFired = true;

        var desc = new OverlayDescriptor
        {
            Id = "test.overlay.custom",
            Title = "Custom Overlay",
            Slot = "shell.overlay",
            DefaultWidth = 300,
            DefaultHeight = 400,
            ViewFactory = _ => "MockView"
        };

        using var unreg = registry.RegisterOverlay(desc);
        Assert.True(registryChangedFired);
        Assert.NotNull(registry.GetOverlay("test.overlay.custom"));
        Assert.Single(registry.GetAllOverlays());

        // Initially not visible
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));

        // Show
        registry.ShowOverlay("test.overlay.custom");
        Assert.True(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Single(registry.ActiveOverlays);
        Assert.Equal("Custom Overlay", registry.ActiveOverlays[0].Title);

        // Toggle (hides)
        registry.ToggleOverlay("test.overlay.custom");
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Empty(registry.ActiveOverlays);

        // Toggle (shows again)
        registry.ToggleOverlay("test.overlay.custom");
        Assert.True(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Single(registry.ActiveOverlays);

        // Hide
        registry.HideOverlay("test.overlay.custom");
        Assert.False(registry.IsOverlayVisible("test.overlay.custom"));
        Assert.Empty(registry.ActiveOverlays);
    }

    [Fact]
    public void SnakeGameViewModel_Initializes_With_Three_Segments()
    {
        using var vm = new SnakeGameViewModel();

        Assert.Equal(3, vm.SnakeBody.Count);
        Assert.Equal(SnakeGameState.Ready, vm.State);
        Assert.Equal(0, vm.Score);
        Assert.True(vm.SnakeBody.First!.Value.X > 0);
        Assert.True(vm.SnakeBody.First!.Value.Y > 0);
    }

    [Fact]
    public void SnakeGameViewModel_Tick_MovesSnake_In_Current_Direction()
    {
        using var vm = new SnakeGameViewModel();
        vm.StartGame();

        var initialHead = vm.SnakeBody.First!.Value;

        // Trigger private tick via reflection to test deterministic step
        var tickMethod = typeof(SnakeGameViewModel).GetMethod("OnGameTick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(tickMethod);

        tickMethod.Invoke(vm, new object?[] { null, EventArgs.Empty });

        var newHead = vm.SnakeBody.First!.Value;
        // Default direction is Right (+1 on X)
        Assert.Equal(initialHead.X + 1, newHead.X);
        Assert.Equal(initialHead.Y, newHead.Y);
        Assert.Equal(3, vm.SnakeBody.Count);
    }

    [Fact]
    public void SnakeGameViewModel_ChangeDirection_Prevents_180Degree_Turn()
    {
        using var vm = new SnakeGameViewModel();
        vm.StartGame();

        // Currently Right. Attempting Left should be ignored.
        vm.ChangeDirection(SnakeDirection.Left);

        var tickMethod = typeof(SnakeGameViewModel).GetMethod("OnGameTick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(tickMethod);
        tickMethod.Invoke(vm, new object?[] { null, EventArgs.Empty });

        // Snake still went right
        var head = vm.SnakeBody.First!.Value;
        Assert.Equal(SnakeGameViewModel.GridWidth / 2 + 1, head.X);
    }

    [Fact]
    public void SnakeGameViewModel_Eating_Food_Increments_Score_And_Length()
    {
        using var vm = new SnakeGameViewModel();
        vm.StartGame();

        var head = vm.SnakeBody.First!.Value;

        // Place food directly in front of snake (head.X + 1, head.Y)
        var foodProp = typeof(SnakeGameViewModel).GetProperty("Food");
        Assert.NotNull(foodProp);
        var targetFood = new GridPoint(head.X + 1, head.Y);
        foodProp.SetValue(vm, targetFood);

        var tickMethod = typeof(SnakeGameViewModel).GetMethod("OnGameTick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(tickMethod);
        tickMethod.Invoke(vm, new object?[] { null, EventArgs.Empty });

        Assert.Equal(10, vm.Score);
        Assert.Equal(4, vm.SnakeBody.Count); // Length increased from 3 to 4
        Assert.Equal(targetFood, vm.SnakeBody.First!.Value);
    }

    [Fact]
    public void SnakeGameViewModel_WallCollision_TriggersGameOver_When_Enabled()
    {
        using var vm = new SnakeGameViewModel
        {
            WallCollision = true
        };
        vm.StartGame();

        // Place head at right edge
        vm.SnakeBody.Clear();
        vm.SnakeBody.AddFirst(new GridPoint(SnakeGameViewModel.GridWidth - 1, 5));
        vm.SnakeBody.AddLast(new GridPoint(SnakeGameViewModel.GridWidth - 2, 5));

        var tickMethod = typeof(SnakeGameViewModel).GetMethod("OnGameTick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(tickMethod);

        // Moving right from edge hits wall
        tickMethod.Invoke(vm, new object?[] { null, EventArgs.Empty });

        Assert.Equal(SnakeGameState.GameOver, vm.State);
        Assert.Contains("wall", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SnakeGameViewModel_WallWrap_WrapsAround_When_Collision_Disabled()
    {
        using var vm = new SnakeGameViewModel
        {
            WallCollision = false
        };
        vm.StartGame();

        // Place head at right edge
        vm.SnakeBody.Clear();
        vm.SnakeBody.AddFirst(new GridPoint(SnakeGameViewModel.GridWidth - 1, 5));
        vm.SnakeBody.AddLast(new GridPoint(SnakeGameViewModel.GridWidth - 2, 5));

        var tickMethod = typeof(SnakeGameViewModel).GetMethod("OnGameTick", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(tickMethod);

        // Moving right wraps to 0
        tickMethod.Invoke(vm, new object?[] { null, EventArgs.Empty });

        Assert.Equal(SnakeGameState.Playing, vm.State);
        Assert.Equal(0, vm.SnakeBody.First!.Value.X);
    }

    [Fact]
    public async Task SnakeGamePlugin_Registers_All_Capabilities_And_Rolls_Back_Cleanly()
    {
        var services = new ServiceCollection();
        var overlayReg = new OverlayRegistry(services.BuildServiceProvider());
        var commandReg = new CommandPaletteRegistry();
        var statusReg = new StatusBarRegistry(seedDefaults: false);
        var ribbonReg = new RibbonRegistry();

        var rootContext = new FryPluginContext();
        rootContext.RegisterService<IOverlayRegistry>(overlayReg);
        rootContext.RegisterService<ICommandPaletteRegistry>(commandReg);
        rootContext.RegisterService<IStatusBarRegistry>(statusReg);
        rootContext.RegisterService<IRibbonRegistry>(ribbonReg);

        var plugin = new SnakeGamePlugin();
        Assert.Equal("frypdf.overlay.snake", plugin.Id);
        Assert.NotNull(plugin.SettingsSchema);
        Assert.True(plugin.SettingsSchema.ContainsKey("InitialSpeed"));
        Assert.True(plugin.SettingsSchema.ContainsKey("WallCollision"));

        using var scope = new PluginScope();
        var scopedContext = rootContext.CreateScopedContext(scope);

        await plugin.ApplyAsync(scopedContext);

        // 1. Verify Overlay registered
        Assert.NotNull(overlayReg.GetOverlay("frypdf.overlay.snake"));

        // 2. Verify Command Palette registered
        Assert.Contains(commandReg.GetAllCommands(), c => c.Id == "cmd.overlay.snake");

        // 3. Verify Status Bar Widget registered
        var rightWidgets = statusReg.GetWidgets(StatusBarAlignment.Right);
        Assert.Contains(rightWidgets, w => w.WidgetId == "frypdf.status.snake");

        // 4. Verify Ribbon Action registered
        var ribbonActions = ribbonReg.GetActionsForTab("view");
        Assert.Contains(ribbonActions, a => a.Id == "frypdf.ribbon.action.snake");

        // 5. Test LIFO Rollback when scope is disposed
        scope.Dispose();

        Assert.Null(overlayReg.GetOverlay("frypdf.overlay.snake"));
        Assert.DoesNotContain(commandReg.GetAllCommands(), c => c.Id == "cmd.overlay.snake");
        Assert.DoesNotContain(statusReg.GetWidgets(StatusBarAlignment.Right), w => w.WidgetId == "frypdf.status.snake");
        Assert.DoesNotContain(ribbonReg.GetActionsForTab("view"), a => a.Id == "frypdf.ribbon.action.snake");
    }

    [Fact]
    public void ShellOverlaysBundle_Contains_SnakePlugin()
    {
        var bundle = new ShellOverlaysBundle();
        Assert.Equal("FryPdf.Bundle.ShellOverlays", bundle.Id);
        Assert.Single(bundle.Plugins);
        Assert.IsType<SnakeGamePlugin>(bundle.Plugins[0]);
    }

    [Fact]
    public void ShellOverlayHost_ClampsAndPositionsOverlays_WithinViewportBounds()
    {
        var services = new ServiceCollection();
        App.ConfigureServices(services);
        var sp = services.BuildServiceProvider();

        var mainVm = sp.GetRequiredService<MainViewModel>();
        var host = new PdfEditorApp.Views.Overlays.ShellOverlayHost
        {
            DataContext = mainVm
        };

        var overlayReg = sp.GetRequiredService<OverlayRegistry>();
        var desc = new OverlayDescriptor
        {
            Id = "test.overlay.positioning",
            Title = "Positioning Test",
            Slot = "shell.overlay",
            DefaultWidth = 320,
            DefaultHeight = 420,
            InitialX = null,
            InitialY = 100,
            ViewFactory = _ => new Avalonia.Controls.Border()
        };

        overlayReg.RegisterOverlay(desc);
        overlayReg.ShowOverlay(desc.Id);

        Assert.Single(mainVm.ActiveOverlays);
        var instance = mainVm.ActiveOverlays[0];

        // Measure and Arrange host with 1200x800
        host.Measure(new Avalonia.Size(1200, 800));
        host.Arrange(new Avalonia.Rect(0, 0, 1200, 800));

        // Clamping docks within 1200x800
        Assert.True(instance.X > 0);
        Assert.True(instance.X + instance.Width <= 1200);
        Assert.True(instance.Y >= 60);
        Assert.True(instance.Y + instance.Height <= 800);
    }
}
