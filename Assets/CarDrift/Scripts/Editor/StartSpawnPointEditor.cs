using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StartSpawnPoint))]
public class StartSpawnPointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StartSpawnPoint spawner = (StartSpawnPoint)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🛠️ SPAWN POINT EDITOR TOOLS", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
        if (GUILayout.Button("✨ Generate / Update Spawn Points", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Generate Spawn Points");
            spawner.GenerateSpawnPointsInEditor();
            EditorUtility.SetDirty(spawner);
        }
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Clear All Spawn Points", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận xóa", "Bạn có chắc chắn muốn xóa toàn bộ các điểm Spawn đã tạo?", "Đồng ý", "Hủy"))
            {
                Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Spawn Points");
                spawner.ClearSpawnPointsInEditor();
                EditorUtility.SetDirty(spawner);
            }
        }

        GUI.backgroundColor = Color.white;
        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
            if (GUILayout.Button("🚗 Spawn Vehicles Now (Test PlayMode)", GUILayout.Height(35)))
            {
                _ = spawner.SpawnAllVehiclesAsync();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
