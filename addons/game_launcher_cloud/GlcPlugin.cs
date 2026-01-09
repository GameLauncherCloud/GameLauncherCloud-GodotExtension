#if TOOLS
using Godot;

namespace GameLauncherCloud;

/// <summary>
/// Main plugin class for Game Launcher Cloud Manager.
/// Handles plugin lifecycle and dock management.
/// </summary>
[Tool]
public partial class GlcPlugin : EditorPlugin
{
    private const string PluginName = "Game Launcher Cloud Manager";
    private const string DockScenePath = "res://addons/game_launcher_cloud/ui/GlcManagerDock.tscn";

    private Control? _dockInstance;

    public override void _EnterTree()
    {
        GD.Print($"[GLC] {PluginName} plugin initialized");

        // Load and instantiate the dock scene
        var dockScene = GD.Load<PackedScene>(DockScenePath);
        if (dockScene != null)
        {
            _dockInstance = dockScene.Instantiate<Control>();
            AddControlToDock(DockSlot.RightUl, _dockInstance);
            GD.Print("[GLC] Manager dock added to editor");
        }
        else
        {
            GD.PrintErr("[GLC] Failed to load dock scene");
        }

        // Add tool menu item
        AddToolMenuItem(PluginName, Callable.From(OnMenuPressed));
    }

    public override void _ExitTree()
    {
        GD.Print($"[GLC] {PluginName} plugin disabled");

        // Remove tool menu item
        RemoveToolMenuItem(PluginName);

        // Remove and free dock
        if (_dockInstance != null)
        {
            RemoveControlFromDocks(_dockInstance);
            _dockInstance.QueueFree();
            _dockInstance = null;
        }
    }

    public override string _GetPluginName()
    {
        return PluginName;
    }

    public override Texture2D? _GetPluginIcon()
    {
        // Try to load custom icon, fall back to built-in
        var customIcon = ResourceLoader.Load<Texture2D>("res://addons/game_launcher_cloud/icons/glc_icon.png");
        if (customIcon != null)
        {
            return customIcon;
        }

        return EditorInterface.Singleton.GetEditorTheme().GetIcon("Node", "EditorIcons");
    }

    private void OnMenuPressed()
    {
        if (_dockInstance != null)
        {
            _dockInstance.GrabFocus();
            GD.Print("[GLC] Manager dock focused");
        }
    }
}
#endif