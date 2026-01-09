# Developer Guide - Game Launcher Cloud Godot Extension

## 🔧 Developer Mode

The Godot extension includes a hidden **Developer Mode** that allows easy switching between environments. This mode is **only for internal development** and should **NOT** be visible to end users.

### Activating Developer Mode

To enable Developer mode:

1. Open your Godot project
2. Navigate to: `addons/game_launcher_cloud/`
3. Manually edit `glc_config.json` (or create it if it doesn't exist)
4. Set `"show_developer_options": true`

Example:
```json
{
  "show_developer_options": true,
  "environment": 0
}
```

5. Reload the plugin or restart Godot
6. You'll now see Developer options in the Manager dock

### Environment Selection

The Developer mode provides quick switching between:

#### 🏠 Development
- **API URL:** `https://localhost:7226/`
- **Frontend URL:** `http://localhost:4200`
- **Database:** Local PostgreSQL
- **Cloudflare R2:** `game-launcher-cloud-development`
- **Stripe:** Test mode

#### 🧪 Staging
- **API URL:** `https://stagingapi.gamelauncher.cloud`
- **Frontend URL:** `https://staging.app.gamelauncher.cloud`
- **Database:** Railway Staging (nozomi)
- **Cloudflare R2:** `game-launcher-cloud-staging`
- **Stripe:** Test mode

#### 🚀 Production
- **API URL:** `https://api.gamelauncher.cloud`
- **Frontend URL:** `https://app.gamelauncher.cloud`
- **Database:** Railway Production (mainline)
- **Cloudflare R2:** `game-launcher-cloud-production`
- **Stripe:** Live mode

### Features in Developer Mode

1. **Environment Dropdown** - Select environment from enum
2. **Quick Action Buttons** - One-click switch to Dev/Staging/Prod
3. **Current Configuration Display** - Shows active API and Frontend URLs
4. **All Environment URLs** - Reference list of all endpoints
5. **Toggle Developer Mode** - Show/hide developer options
6. **Clear Auth Data** - Quick logout and credential clearing

### Configuration Structure

```gdscript
enum GLCEnvironment {
    PRODUCTION = 0,
    STAGING = 1,
    DEVELOPMENT = 2
}

class GLCConfig:
    var api_key_production: String = ""
    var api_key_staging: String = ""
    var api_key_development: String = ""
    var auth_token: String = ""
    var user_id: String = ""
    var user_email: String = ""
    var user_plan: String = ""
    var selected_app_id: int = 0
    var selected_app_name: String = ""
    var environment: int = GLCEnvironment.PRODUCTION
    var show_developer_options: bool = false
    
    func get_api_url() -> String:
        match environment:
            GLCEnvironment.DEVELOPMENT:
                return "https://127.0.0.1:7226"
            GLCEnvironment.STAGING:
                return "https://stagingapi.gamelauncher.cloud"
            _:
                return "https://api.gamelauncher.cloud"
    
    func get_frontend_url() -> String:
        match environment:
            GLCEnvironment.DEVELOPMENT:
                return "http://localhost:4200"
            GLCEnvironment.STAGING:
                return "https://staging.app.gamelauncher.cloud"
            _:
                return "https://app.gamelauncher.cloud"
```

### Before Publishing

**⚠️ CRITICAL:** Before creating a release build for end users:

1. Open the Developer options
2. Set **Environment** to `Production`
3. Set **Show Developer Options** to `false` (unchecked)
4. Save and test
5. Verify the Developer options are no longer visible
6. Create the release package

This ensures users don't see internal development options.

### Testing Workflow

#### Local Development Testing
```
1. Set environment to "Development"
2. Run local backend (dotnet run)
3. Run local frontend (ng serve)
4. Test builds upload to local R2 bucket
```

#### Staging Testing
```
1. Set environment to "Staging"
2. Use staging API keys
3. Test with staging database
4. Verify uploads go to staging R2
```

#### Production Testing
```
1. Set environment to "Production"
2. Use production API keys (carefully!)
3. Test end-to-end flow
4. Verify real builds are created
```

### API Key Management

Different environments require different API keys:

- **Development:** Use test API keys from local database
- **Staging:** Use staging API keys from staging.app.gamelauncher.cloud
- **Production:** Use production API keys from app.gamelauncher.cloud

The extension automatically connects to the correct backend based on the selected environment.

### Troubleshooting

#### Developer Options Not Showing
- Check `glc_config.json` has `"show_developer_options": true`
- Restart Godot Editor
- Disable and re-enable the plugin

#### SSL Certificate Errors in Development
- The extension bypasses SSL validation for localhost/127.0.0.1
- For staging/production, valid SSL certificates are required

#### HTTP Errors
- Check the Godot output console for detailed error messages
- Verify the backend is running for the selected environment
- Check your API key is valid for the selected environment
