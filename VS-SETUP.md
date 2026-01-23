# Visual Studio Setup Guide

This guide will help you open this repository in Visual Studio and start making changes.

## Prerequisites

Before you begin, make sure you have:
- **Visual Studio 2022** (Community, Professional, or Enterprise)
- **.NET 9.0 SDK** or later
- **Git** (usually installed with Visual Studio)

## Opening the Project in Visual Studio

### Method 1: Clone from GitHub (Recommended for first time)

1. **Open Visual Studio 2022**
2. On the start window, click **"Clone a repository"**
3. In the "Repository location" field, enter:
   ```
   https://github.com/GiXxXmO/matsedel-rss.git
   ```
4. Choose a local path where you want to save the project
5. Click **"Clone"**
6. Visual Studio will automatically detect and open the `MatsedelRss.sln` solution file

### Method 2: Open Existing Local Repository

If you've already cloned the repository using Git:

1. **Open Visual Studio 2022**
2. Click **"Open a project or solution"**
3. Navigate to your cloned repository folder
4. Select **`MatsedelRss.sln`**
5. Click **"Open"**

### Method 3: Open from File Explorer

Simply double-click the **`MatsedelRss.sln`** file in your file explorer, and Visual Studio will open the project.

## Understanding the Solution Structure

Once opened, you'll see the solution structure in the **Solution Explorer**:

```
MatsedelRss (Solution)
└── MatsedelRss (Project)
    ├── Dependencies
    ├── Program.cs           # Main application code
    ├── MatsedelRss.csproj  # Project configuration
    ├── viewer.html          # HTML viewer for digital signage
    └── output/              # Generated RSS feeds (after running)
```

## Making Changes

### 1. Edit Code Files

- Click on any file in the **Solution Explorer** to open it in the editor
- Make your changes
- Visual Studio will automatically show:
  - **Syntax highlighting**
  - **IntelliSense** (code completion)
  - **Error detection**

### 2. Build the Project

Before committing, always build to check for errors:

- **Menu**: Build → Build Solution
- **Keyboard**: `Ctrl + Shift + B`
- **Check the Output window** for any build errors

### 3. Run the Project

To test your changes:

- **Menu**: Debug → Start Without Debugging
- **Keyboard**: `Ctrl + F5`
- The program will run and generate RSS feeds in the `output/` folder

## Committing Changes to Git

Visual Studio has built-in Git support. Here's how to commit your changes:

### Step 1: View Changes

1. Open **Git Changes** window:
   - **Menu**: View → Git Changes
   - **Keyboard**: `Ctrl + 0, Ctrl + G`

2. You'll see all modified files listed under "Changes"

### Step 2: Stage Changes

- To stage all changes: Click the **+** icon next to "Changes"
- To stage specific files: Click the **+** icon next to each file
- Staged files appear under "Staged Changes"

### Step 3: Write Commit Message

1. In the **commit message box** at the top, write a descriptive message
   - Example: `"Add new feature for filtering allergies"`
   - Example: `"Fix parsing error for special characters"`

2. Keep it clear and concise

### Step 4: Commit

- Click **"Commit Staged"** to commit locally
- Or click **"Commit Staged and Push"** to commit and push to GitHub in one step

### Step 5: Push to GitHub (if not done in Step 4)

1. After committing, click the **↑** (up arrow) button in the Git Changes window
2. Or use: **Menu** → Git → Push

## Working with Branches

### Create a New Branch

1. In the Git Changes window, click the branch name at the top
2. Click **"New Branch"**
3. Enter a branch name (e.g., `feature/my-new-feature`)
4. Click **"Create"**

### Switch Between Branches

1. Click the current branch name at the top of Git Changes
2. Select the branch you want to switch to

### Merge Changes

1. Switch to the branch you want to merge into (usually `main`)
2. **Menu**: Git → Manage Branches
3. Right-click the branch you want to merge
4. Select **"Merge [branch] into current branch"**

## Pulling Latest Changes

To get the latest changes from GitHub:

1. **Menu**: Git → Pull
2. **Or** click the **↓** (down arrow) in the Git Changes window

## Troubleshooting

### "The project file could not be loaded"

**Solution**: Make sure you have .NET 9.0 SDK installed. Download from: https://dotnet.microsoft.com/download

### "NuGet packages need to be restored"

**Solution**: Right-click the solution → **Restore NuGet Packages**

### "Git authentication failed"

**Solution**: 
1. Go to **Tools → Options → Source Control → Git Global Settings**
2. Configure your name and email
3. Use Visual Studio's built-in authentication when prompted

### Changes not showing in Git Changes window

**Solution**: 
1. Make sure you're in the correct repository
2. Check that files aren't ignored by `.gitignore`
3. Try: **Git → Refresh** in the menu

## Best Practices

### Before Making Changes
1. **Pull latest changes** from GitHub
2. **Create a new branch** for your feature/fix
3. Make sure the project **builds successfully**

### While Working
1. **Commit frequently** with clear messages
2. **Build and test** your changes regularly
3. Use **descriptive branch names**

### Before Pushing
1. **Build the solution** to check for errors
2. **Test your changes** by running the application
3. **Review your changes** in the Git Changes window
4. Write a **clear commit message**

## Additional Resources

- [Visual Studio Git Tutorial](https://docs.microsoft.com/en-us/visualstudio/version-control/)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Project README](README.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Quick Start Guide](QUICKSTART.md)

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Build Solution | `Ctrl + Shift + B` |
| Run without Debugging | `Ctrl + F5` |
| Start Debugging | `F5` |
| Git Changes Window | `Ctrl + 0, Ctrl + G` |
| Solution Explorer | `Ctrl + Alt + L` |
| Find in Files | `Ctrl + Shift + F` |
| Go to Definition | `F12` |
| Comment/Uncomment | `Ctrl + K, Ctrl + C` / `Ctrl + K, Ctrl + U` |

## Need Help?

If you encounter any issues:
1. Check the [Troubleshooting](#troubleshooting) section above
2. Review the project documentation
3. Open an issue on GitHub with details about your problem

Happy coding! 🚀
