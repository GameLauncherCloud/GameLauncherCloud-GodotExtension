<div align="center">
   
# Game Launcher Cloud - Manager for Godot
### **The Next-Generation Custom Game Launchers Creator Platform**

<img width="2800" height="720" alt="Game Launcher Cloud for Godot Game Engine - Full Logo with Background" src="https://github.com/user-attachments/assets/2be3e88f-bdc2-4771-9c8b-a77c63d819bd" />

**Build and upload your game from Godot Engine to Game Launcher Cloud!**

[![Website](https://img.shields.io/badge/Website-gamelauncher.cloud-blue?style=for-the-badge&logo=internet-explorer)](https://gamelauncher.cloud/)
[![Status](https://img.shields.io/badge/Status-Live-success?style=for-the-badge)](https://gamelauncher.cloud/)
[![Platform](https://img.shields.io/badge/Platform-Cross--Platform-orange?style=for-the-badge)](https://gamelauncher.cloud/)
[![Godot](https://img.shields.io/badge/Godot-4.x-478CBF?style=for-the-badge&logo=godot-engine)](https://godotengine.org/)

</div>

**Build and upload your game from Godot Engine to Game Launcher Cloud!**

A powerful Godot Editor plugin that allows you to build and upload your game patches directly to [Game Launcher Cloud](https://gamelauncher.cloud) platform from within the Godot Editor.

## 🌟 Features

### ✓ **Connect to Your Account**
- Easy authentication using **API Key**
- Secure connection to Game Launcher Cloud backend
- Persistent login sessions

### ✓ **Build and Upload Patches**
- Build your Godot game directly from the editor
- Automatic compression and optimization
- Upload builds to Game Launcher Cloud with one click
- Real-time upload progress tracking
- Support for Windows, Linux, and macOS builds

### ✓ **Tips and Best Practices**
- Receive helpful tips to improve patch quality
- Learn optimization techniques
- Best practices for game distribution
- Build size recommendations

## 📦 Installation

### Method 1: Asset Library (Recommended)
1. Open Godot Editor
2. Go to **AssetLib** tab
3. Search for "Game Launcher Cloud"
4. Click **Download** and **Install**
5. Enable the plugin in **Project > Project Settings > Plugins**

### Method 2: Manual Installation
1. Download the latest release from [GitHub Releases](https://github.com/GameLauncherCloud/GameLauncherCloud-GodotExtension/releases)
2. Extract the `addons/game_launcher_cloud` folder into your project's `addons/` directory
3. Enable the plugin in **Project > Project Settings > Plugins**

## 🚀 Quick Start

### Step 1: Get Your API Key

1. Go to [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud)
2. Navigate to **User Profile > API Keys**
3. Click **Create New API Key**
4. Copy your API key

### Step 2: Connect to Game Launcher Cloud

1. In Godot, open **Project > Tools > Game Launcher Cloud Manager**
2. Go to the **Login** tab
3. Paste your API Key
4. Click **Login with API Key**

### Step 3: Build and Upload

1. Go to the **Build & Upload** tab
2. Click **Load My Apps** to see your available apps
3. Select the app you want to upload to
4. Write some **Build Notes** describing what changed
5. Click **Build & Upload to Game Launcher Cloud**
6. Wait for the build and upload to complete!

## 📖 Documentation

### Authentication

The extension supports authentication via **API Keys**. This is the recommended method for automated builds and CI/CD pipelines.

To get an API Key:
- Visit [Game Launcher Cloud Dashboard](https://app.gamelauncher.cloud/user/api-keys)
- Create a new API key with appropriate permissions
- Copy and save the key securely

### Building and Uploading

The extension will:
1. Build your Godot project for the currently selected platform
2. Compress the build into a ZIP file
3. Upload it to Game Launcher Cloud
4. Process the patch automatically on the server

**Supported Platforms:**
- Windows (64-bit)
- Linux (64-bit)
- macOS

### Configuration

The extension saves your settings in:
```
res://addons/game_launcher_cloud/glc_config.json
```

**Note:** Add this file to `.gitignore` to avoid committing your API key!

## 💡 Tips for Better Patches

### Optimize Build Size
- Compress textures appropriately
- Remove unused assets
- Use PCK files for large content
- Enable script encryption if needed

### Use Descriptive Build Notes
Always include:
- Version number
- New features added
- Bugs fixed
- Known issues

### Test Before Uploading
- Run the build locally first
- Check for crashes or errors
- Verify all features work
- Test performance

## 🔧 Requirements

- **Godot 4.0** or newer
- Active **Game Launcher Cloud** account
- Export templates installed for your target platform

## 🤝 Support

Need help? We're here for you!

- 🌐 Website: [gamelauncher.cloud](https://gamelauncher.cloud)
- 💬 Discord: [Join our community](https://discord.com/invite/FpWvUQ2CJP)
- 📚 Documentation: [docs.gamelauncher.cloud](https://help.gamelauncher.cloud)

## 📝 License

This extension is provided free of charge for use with Game Launcher Cloud platform.

## 🎮 About Game Launcher Cloud

Game Launcher Cloud is a comprehensive platform for game developers to:
- Create custom game launchers in minutes
- Distribute game patches efficiently
- Manage multiple games and versions
- Track downloads and analytics
