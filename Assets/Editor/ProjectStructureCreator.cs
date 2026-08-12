using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Script Editor pour créer automatiquement toute la structure de dossiers du projet BeeKingdom
/// INSTRUCTIONS:
/// 1. Placer ce script dans Assets/Editor/
/// 2. Dans Unity, aller dans le menu: Tools > Create Project Structure
/// 3. Attendre que les dossiers soient créés
/// 4. Ce script peut être supprimé après utilisation
/// </summary>
public class ProjectStructureCreator : EditorWindow
{
    [MenuItem("Tools/Create Bee Kingdom Project Structure")]
    public static void CreateProjectStructure()
    {
        if (EditorUtility.DisplayDialog(
            "Create Project Structure",
            "This will create the complete folder structure for Bee Kingdom.\n\nContinue?",
            "Yes, Create!",
            "Cancel"))
        {
            CreateFolders();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success!", "Project structure created successfully! 🐝", "OK");
        }
    }

    private static void CreateFolders()
    {
        string[] folders = new string[]
        {
            // Main Project folder
            "Assets/_Project",
            
            // Scripts structure
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Core/Managers",
            "Assets/_Project/Scripts/Core/Data",
            "Assets/_Project/Scripts/Core/Events",
            "Assets/_Project/Scripts/Core/Utilities",
            
            "Assets/_Project/Scripts/Gameplay",
            "Assets/_Project/Scripts/Gameplay/Bees",
            "Assets/_Project/Scripts/Gameplay/Buildings",
            "Assets/_Project/Scripts/Gameplay/Resources",
            "Assets/_Project/Scripts/Gameplay/Combat",
            "Assets/_Project/Scripts/Gameplay/Progression",
            
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/UI/Screens",
            "Assets/_Project/Scripts/UI/Panels",
            "Assets/_Project/Scripts/UI/Elements",
            "Assets/_Project/Scripts/UI/HUD",
            
            "Assets/_Project/Scripts/Audio",
            
            // Prefabs structure
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/Bees",
            "Assets/_Project/Prefabs/Buildings",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Prefabs/Effects",
            "Assets/_Project/Prefabs/Enemies",
            
            // Scenes
            "Assets/_Project/Scenes",
            
            // ScriptableObjects
            "Assets/_Project/ScriptableObjects",
            "Assets/_Project/ScriptableObjects/Bees",
            "Assets/_Project/ScriptableObjects/Buildings",
            "Assets/_Project/ScriptableObjects/Enemies",
            "Assets/_Project/ScriptableObjects/Resources",
            "Assets/_Project/ScriptableObjects/GameConfig",
            
            // Data
            "Assets/_Project/Data",
            "Assets/_Project/Data/Balancing",
            "Assets/_Project/Data/Localization",
            
            // Resources (for runtime loading)
            "Assets/_Project/Resources",
            "Assets/_Project/Resources/Icons",
            "Assets/_Project/Resources/Audio",
            
            // Art folders
            "Assets/Art",
            "Assets/Art/Sprites",
            "Assets/Art/Sprites/Bees",
            "Assets/Art/Sprites/Buildings",
            "Assets/Art/Sprites/UI",
            "Assets/Art/Sprites/Environment",
            "Assets/Art/Animations",
            "Assets/Art/Materials",
            "Assets/Art/Shaders",
            
            // Audio folders
            "Assets/Audio",
            "Assets/Audio/Music",
            "Assets/Audio/SFX",
            "Assets/Audio/Mixers",
            
            // Plugins (for third-party SDKs)
            "Assets/Plugins",
            "Assets/Plugins/Android",
            "Assets/Plugins/iOS",
            
            // Editor folder (for editor scripts)
            "Assets/Editor"
        };

        int createdCount = 0;
        int skippedCount = 0;

        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                createdCount++;
                Debug.Log($"✅ Created: {folder}");
            }
            else
            {
                skippedCount++;
                Debug.Log($"⏭️ Already exists: {folder}");
            }
        }

        Debug.Log($"\n🐝 <b>Bee Kingdom Project Structure Created!</b>");
        Debug.Log($"📁 Created {createdCount} new folders");
        Debug.Log($"⏭️ Skipped {skippedCount} existing folders");
        Debug.Log($"✅ Total structure: {folders.Length} folders\n");
        
        // Create a README file in the _Project folder
        CreateReadmeFile();
    }

    private static void CreateReadmeFile()
    {
        string readmePath = "Assets/_Project/README.txt";
        string readmeContent = @"BEE KINGDOM - Project Structure
================================

This folder contains all the game code and assets for Bee Kingdom.

FOLDER STRUCTURE:
-----------------

📁 Scenes/          - All Unity scenes
📁 Scripts/         - All C# scripts
   📁 Core/         - Core systems (Managers, Events, Utilities)
   📁 Gameplay/     - Gameplay logic (Bees, Buildings, Combat, etc.)
   📁 UI/           - User interface scripts
   📁 Audio/        - Audio management scripts

📁 Prefabs/         - All prefabs (Bees, Buildings, UI, etc.)
📁 ScriptableObjects/ - Data definitions (Bees, Buildings, etc.)
📁 Data/            - Static data (Balance, Localization)
📁 Resources/       - Runtime-loadable assets

EXTERNAL FOLDERS:
-----------------
📁 Art/             - All visual assets (Sprites, Animations, etc.)
📁 Audio/           - Music and sound effects
📁 Plugins/         - Third-party SDKs and plugins
📁 Editor/          - Unity Editor scripts

Created with ❤️ for Bee Kingdom 🐝

For architecture details, see the Technical Architecture document.
";

        File.WriteAllText(readmePath, readmeContent);
        Debug.Log($"📝 Created README at: {readmePath}");
    }
}
