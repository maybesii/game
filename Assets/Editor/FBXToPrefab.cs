using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

public class FBXToMeshAndPrefabAutomation : EditorWindow
{
    private string fbxFolderPath = "Assets/FBX"; // Папка с FBX файлами
    private string meshOutputPath = "Assets/Meshes"; // Папка для сохранения мешей
    private string prefabOutputPath = "Assets/Prefabs"; // Папка для сохранения префабов

    [MenuItem("Tools/Automate FBX to Meshes and Prefabs")]
    static void OpenWindow()
    {
        GetWindow<FBXToMeshAndPrefabAutomation>("Automate FBX to Meshes and Prefabs");
    }

    void OnGUI()
    {
        GUILayout.Label("Automate FBX to Meshes and Prefabs", EditorStyles.boldLabel);
        fbxFolderPath = EditorGUILayout.TextField("FBX Folder Path", fbxFolderPath);
        meshOutputPath = EditorGUILayout.TextField("Mesh Output Path", meshOutputPath);
        prefabOutputPath = EditorGUILayout.TextField("Prefab Output Path", prefabOutputPath);

        if (GUILayout.Button("Process FBX Files"))
        {
            ProcessFBXFiles();
        }
    }

    void ProcessFBXFiles()
    {
        // Проверка существования папок
        if (!AssetDatabase.IsValidFolder(fbxFolderPath))
        {
            Debug.LogError($"FBX folder {fbxFolderPath} does not exist!");
            return;
        }
        if (!AssetDatabase.IsValidFolder(meshOutputPath))
        {
            Directory.CreateDirectory(meshOutputPath);
            AssetDatabase.Refresh();
        }
        if (!AssetDatabase.IsValidFolder(prefabOutputPath))
        {
            Directory.CreateDirectory(prefabOutputPath);
            AssetDatabase.Refresh();
        }

        // Поиск всех FBX файлов
        string[] fbxFiles = Directory.GetFiles(fbxFolderPath, "*.fbx", SearchOption.AllDirectories);
        if (fbxFiles.Length == 0)
        {
            Debug.LogWarning($"No FBX files found in {fbxFolderPath}!");
            return;
        }

        foreach (string fbxPath in fbxFiles)
        {
            string assetPath = fbxPath.Replace(Application.dataPath, "Assets").Replace("\\", "/");
            OptimizeFBXImportSettings(assetPath);
            ExtractMeshesAndCreatePrefabs(assetPath);
            DeleteFBXFile(assetPath);
        }

        Debug.Log("FBX processing completed!");
        AssetDatabase.Refresh();
    }

    void OptimizeFBXImportSettings(string fbxPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"Failed to load ModelImporter for {fbxPath}");
            return;
        }

        // Оптимизация настроек импорта
        importer.meshCompression = ModelImporterMeshCompression.High;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
        importer.isReadable = false; // Отключить Read/Write для экономии памяти
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;

        importer.SaveAndReimport();
        Debug.Log($"Optimized import settings for {fbxPath}");
    }

    void ExtractMeshesAndCreatePrefabs(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Mesh[] meshes = assets.OfType<Mesh>().ToArray();

        foreach (Mesh mesh in meshes)
        {
            // Сохранить меш как отдельный ассет
            string meshName = mesh.name;
            string meshPath = $"{meshOutputPath}/{meshName}.asset";
            int suffix = 1;
            while (File.Exists(meshPath))
            {
                meshPath = $"{meshOutputPath}/{meshName}_{suffix}.asset";
                suffix++;
            }

            AssetDatabase.CreateAsset(Object.Instantiate(mesh), meshPath);
            Debug.Log($"Saved mesh: {meshPath}");

            // Создать GameObject с этим мешом
            GameObject instance = new GameObject(meshName);
            MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            instance.AddComponent<MeshRenderer>(); // Добавляем рендер, материал нужно назначить вручную

            // Сохранить как префаб
            string prefabPath = $"{prefabOutputPath}/{meshName}.prefab";
            suffix = 1;
            while (File.Exists(prefabPath))
            {
                prefabPath = $"{prefabOutputPath}/{meshName}_{suffix}.prefab";
                suffix++;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out bool success);
            if (success)
                Debug.Log($"Created prefab: {prefabPath}");
            else
                Debug.LogError($"Failed to create prefab: {prefabPath}");

            DestroyImmediate(instance);
        }
    }

    void DeleteFBXFile(string fbxPath)
    {
        // Удалить FBX файл только после успешной обработки
        if (AssetDatabase.DeleteAsset(fbxPath))
            Debug.Log($"Deleted FBX file: {fbxPath}");
        else
            Debug.LogError($"Failed to delete FBX file: {fbxPath}");
    }
}