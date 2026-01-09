#if TOOLS
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace GameLauncherCloud;

/// <summary>
/// Environment types for Game Launcher Cloud
/// </summary>
public enum GlcEnvironment
{
	Production = 0,
	Staging = 1,
	Development = 2
}

/// <summary>
/// Configuration data for Game Launcher Cloud Godot Extension.
/// Stores authentication credentials and app settings.
/// </summary>
public class GlcConfig
{
	private const string ConfigPath = "res://addons/game_launcher_cloud/glc_config.json";

	// Environment-specific API Keys
	[JsonPropertyName("api_key_production")]
	public string ApiKeyProduction { get; set; } = "";

	[JsonPropertyName("api_key_staging")]
	public string ApiKeyStaging { get; set; } = "";

	[JsonPropertyName("api_key_development")]
	public string ApiKeyDevelopment { get; set; } = "";

	[JsonPropertyName("auth_token")]
	public string? AuthToken { get; set; }

	[JsonPropertyName("user_id")]
	public string? UserId { get; set; }

	[JsonPropertyName("user_email")]
	public string? UserEmail { get; set; }

	[JsonPropertyName("user_plan")]
	public string? UserPlan { get; set; }

	[JsonPropertyName("selected_app_id")]
	public long SelectedAppId { get; set; } = 0;

	[JsonPropertyName("selected_app_name")]
	public string? SelectedAppName { get; set; }

	[JsonPropertyName("remember_me")]
	public bool RememberMe { get; set; } = true;

	[JsonPropertyName("build_notes")]
	public string? BuildNotes { get; set; }

	[JsonPropertyName("last_build_path")]
	public string LastBuildPath { get; set; } = "";

	[JsonPropertyName("selected_export_preset")]
	public int SelectedExportPreset { get; set; } = 0;

	[JsonPropertyName("environment")]
	public GlcEnvironment Environment { get; set; } = GlcEnvironment.Production;

	[JsonPropertyName("show_developer_options")]
	public bool ShowDeveloperOptions { get; set; } = false;

	/// <summary>
	/// Get API Key for the current environment
	/// </summary>
	public string GetApiKey()
	{
		return Environment switch
		{
			GlcEnvironment.Development => string.IsNullOrEmpty(ApiKeyDevelopment) ? "" : ApiKeyDevelopment,
			GlcEnvironment.Staging => string.IsNullOrEmpty(ApiKeyStaging) ? "" : ApiKeyStaging,
			_ => string.IsNullOrEmpty(ApiKeyProduction) ? "" : ApiKeyProduction
		};
	}

	/// <summary>
	/// Set API Key for the current environment
	/// </summary>
	public void SetApiKey(string key)
	{
		switch (Environment)
		{
			case GlcEnvironment.Development:
				ApiKeyDevelopment = key;
				break;
			case GlcEnvironment.Staging:
				ApiKeyStaging = key;
				break;
			default:
				ApiKeyProduction = key;
				break;
		}
	}

	/// <summary>
	/// Get API URL based on selected environment
	/// </summary>
	public string GetApiUrl()
	{
		return Environment switch
		{
			GlcEnvironment.Development => "https://127.0.0.1:7226",
			GlcEnvironment.Staging => "https://stagingapi.gamelauncher.cloud",
			_ => "https://api.gamelauncher.cloud"
		};
	}

	/// <summary>
	/// Get Frontend URL based on selected environment
	/// </summary>
	public string GetFrontendUrl()
	{
		return Environment switch
		{
			GlcEnvironment.Development => "http://localhost:4200",
			GlcEnvironment.Staging => "https://staging.app.gamelauncher.cloud",
			_ => "https://app.gamelauncher.cloud"
		};
	}

	/// <summary>
	/// Check if user is authenticated
	/// </summary>
	public bool IsAuthenticated => !string.IsNullOrEmpty(AuthToken);

	/// <summary>
	/// Clear authentication data (keeps API Key for re-login)
	/// </summary>
	public void ClearAuth()
	{
		AuthToken = null;
		UserId = null;
		UserEmail = null;
		UserPlan = null;
	}
}

/// <summary>
/// Configuration manager for Game Launcher Cloud Godot Extension.
/// Handles saving and loading user settings.
/// </summary>
public static class GlcConfigManager
{
	private const string ConfigPath = "res://addons/game_launcher_cloud/glc_config.json";
	private static GlcConfig? _cachedConfig;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
	};

	/// <summary>
	/// Load configuration from disk
	/// </summary>
	public static GlcConfig LoadConfig()
	{
		if (_cachedConfig != null)
		{
			return _cachedConfig;
		}

		var globalPath = ProjectSettings.GlobalizePath(ConfigPath);

		if (FileAccess.FileExists(ConfigPath))
		{
			try
			{
				using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
				if (file != null)
				{
					var json = file.GetAsText();
					_cachedConfig = JsonSerializer.Deserialize<GlcConfig>(json, JsonOptions);
					if (_cachedConfig != null)
					{
						return _cachedConfig;
					}
				}
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"[GLC] Failed to load config: {ex.Message}");
			}
		}

		// Return default config
		_cachedConfig = new GlcConfig();
		return _cachedConfig;
	}

	/// <summary>
	/// Save configuration to disk
	/// </summary>
	public static void SaveConfig(GlcConfig config)
	{
		try
		{
			var json = JsonSerializer.Serialize(config, JsonOptions);

			using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Write);
			if (file != null)
			{
				file.StoreString(json);
				_cachedConfig = config;
				GD.Print("[GLC] Configuration saved successfully");
			}
			else
			{
				GD.PrintErr($"[GLC] Failed to open config file for writing: {FileAccess.GetOpenError()}");
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[GLC] Failed to save config: {ex.Message}");
		}
	}

	/// <summary>
	/// Clear cached config to force reload
	/// </summary>
	public static void ClearCache()
	{
		_cachedConfig = null;
	}

	/// <summary>
	/// Clear authentication data (keeps API Key for re-login)
	/// </summary>
	public static void ClearAuth()
	{
		var config = LoadConfig();
		config.ClearAuth();
		SaveConfig(config);
	}

	/// <summary>
	/// Check if user is authenticated
	/// </summary>
	public static bool IsAuthenticated()
	{
		var config = LoadConfig();
		return config.IsAuthenticated;
	}
}
#endif
