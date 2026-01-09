# Game Launcher Cloud - Manager for Godot
## Quick Start Guide

### 📋 Prerequisites

Before you start, make sure you have:
- ✅ Godot 4.5.1 or newer with .NET support installed
- ✅ A Game Launcher Cloud account ([Sign up here](https://app.gamelauncher.cloud))
- ✅ At least one app created in your Game Launcher Cloud dashboard
- ✅ Export templates installed for your target platform

### 🔑 Step 1: Get Your API Key

1. Log in to [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
2. Navigate to **Settings** → **API Keys**
3. Click **Create New API Key**
4. Give it a name (e.g., "Godot Extension")
5. **Copy the API key** (you won't be able to see it again!)

### 📥 Step 2: Install the Extension

#### Option A: Asset Library (Recommended)
1. Open Godot Editor
2. Go to the **AssetLib** tab
3. Search for "Game Launcher Cloud"
4. Click **Download** → **Install**
5. Go to **Project** → **Project Settings** → **Plugins**
6. Enable "Game Launcher Cloud Manager"

#### Option B: Manual Installation
1. Download the latest release from [GitHub Releases](https://github.com/GameLauncherCloud/GameLauncherCloud-GodotExtension/releases)
2. Extract the contents
3. Copy the `addons/game_launcher_cloud` folder into your project's `addons/` directory
4. Go to **Project** → **Project Settings** → **Plugins**
5. Enable "Game Launcher Cloud Manager"

### 🚀 Step 3: Open the Manager

1. In Godot, go to the top menu
2. Click **Project** → **Tools** → **Game Launcher Cloud Manager**
3. A new dock window will appear

### 🔐 Step 4: Login

1. In the Manager dock, you'll see the **Login** section
2. Paste your API Key in the text field
3. Click **Login with API Key**
4. You should see "Login successful!" message

### 🎮 Step 5: Configure Export

Before uploading, make sure you have:
1. At least one export preset configured in **Project** → **Export**
2. Export templates installed for your target platform

### 📤 Step 6: Build and Upload Your First Patch

1. In the Manager dock, go to the **Build & Upload** section
2. Click **Load My Apps** to fetch your available apps
3. Select the app you want to upload to from the dropdown
4. Select the export preset to use
5. (Optional) Write some build notes describing what changed
6. Click **Build & Upload to Game Launcher Cloud**
7. Wait for the process to complete (this may take a few minutes depending on build size)
8. Once done, you'll see a success message!

### 💡 Step 7: Learn Best Practices

- Optimize your build size by removing unused assets
- Use descriptive build notes with version numbers
- Test your builds locally before uploading
- Consider using PCK files for large content updates

### 🎉 You're Done!

Your game build has been uploaded to Game Launcher Cloud and is being processed. You can now:
- View your build in the [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
- Share it with your team
- Distribute it to players
- Track downloads and analytics

---

## 🆘 Troubleshooting

### "Login failed" Error
- Make sure you copied the entire API key
- Check that your API key hasn't expired
- Verify you have an active Game Launcher Cloud subscription

### "No apps found" Message
- Make sure you've created at least one app in your Game Launcher Cloud dashboard
- Check that you're logged in with the correct account
- Try clicking **Refresh Apps**

### Build Fails
- Ensure you have export templates installed
- Check that you have a valid export preset configured
- Verify you have enough disk space
- Check Godot's output console for error messages

### Upload Fails
- Check your internet connection
- Verify the build size is within your plan limits
- Make sure you have enough storage quota
- Try uploading again (temporary network issues)

### Plugin Not Showing in Menu
- Check that the plugin files are in `addons/game_launcher_cloud/`
- Verify the plugin is enabled in Project Settings → Plugins
- Try restarting Godot Editor

### Export Templates Not Found
1. Go to **Editor** → **Manage Export Templates**
2. Click **Download and Install** for your Godot version
3. Wait for the download to complete
4. Try exporting again

