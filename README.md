# Unity Game Project (Team Guide)

## Overview & Technology

This project is a **Unity-based game** built with a focus on modularity and scalability.

* **Game Engine:** Unity (2021.3 or later recommended)
* **Programming Language:** C#
* **Version Control:** Git (GitHub for repository management)

---

## 📋 Requirements

The project requires the following setup:

1. **Unity Editor**: Install the appropriate version of Unity via Unity Hub.
2. **Git**: Ensure Git is installed and configured for version control.
3. **Dependencies**: Any external libraries or assets will be managed via Unity's Package Manager.

---

## 🌱 Setting Up the Project

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/your-repo/unity-game-project.git
cd unity-game-project
```

### 2️⃣ Open the Project in Unity

1. Launch Unity Hub.
2. Click **Add Project** and select the cloned project folder.
3. Open the project in the Unity Editor.

### 3️⃣ Install Dependencies

Unity will automatically resolve dependencies via the Package Manager. If prompted, allow Unity to download and install required packages.

---

## Running the Game

1. Open the Unity Editor.
2. Select the **Main Scene** from the `Assets/Scenes` folder.
3. Click the **Play** button in the Unity Editor to run the game.

---

## Git Workflow (IMPORTANT)

**Golden Rule:** Never push directly to the `main` or `dev` branches.

### Step 1: Start a New Feature

```bash
git checkout dev
git pull origin dev
```

### Step 2: Create a Feature Branch

```bash
git checkout -b feature/feature-name
```

### Step 3: Commit Regularly

```bash
git add .
git commit -m "feat: Implemented player movement system"
```

> Tip: Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

### Step 4: Push Your Branch

```bash
git push origin feature/feature-name
```

### Step 5: Create a Pull Request (PR)

1. Go to the repository on GitHub.
2. Create a PR from your branch to the `dev` branch.
3. Add a detailed description, tag reviewers, and merge after approval.

---

## 📂 Project Structure

```
/
├── Assets/           # Game assets (scripts, scenes, prefabs, etc.)
│   ├── Scenes/       # Unity scenes
│   │   ├──GameScenes # Scene ready to use
│   │   └──TestScenes # Scenes for testing new feature
│   ├── Scripts/      # C# scripts
│   ├── Prefabs/      # Prefabricated objects
│   └── ...           # Other asset folders
├── ProjectSettings/  # Unity project settings
├── .gitignore        # Git ignore file
├── README.md         # Project documentation
└── ...               # Other Unity project files
```

---

## ✍️ Code Standards

* **Code Formatter:** Use Unity's built-in C# formatting tools.
* **Linter:** [Rider](https://www.jetbrains.com/rider/) or Visual Studio with proper C# linting rules.
* **Naming Conventions:** Follow Unity's [C# coding standards](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

---

## 🔹 Important Notes

1. **Do not commit large files** (e.g., `.exe`, `.apk`, or `.unitypackage`) to the repository.
2. Always pull the latest changes from the `dev` branch before starting new work.
3. When Testing new Feature Remember to create your own test Scenes. 
3. Keep your commits small and focused on a single task or feature.
4. Test your changes thoroughly before creating a PR.

---

## 📜 Additional Resources

* [Unity Documentation](https://docs.unity3d.com/)
* [C# Programming Guide](https://learn.microsoft.com/en-us/dotnet/csharp/)
* [GitHub Workflow Guide](https://guides.github.com/introduction/flow/)

---