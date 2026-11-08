using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(DocumentsPanel))]
public class DocumentsPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Viewers use this menu's position and size. PDF pages are sprites. " +
            "Videos need an imported clip, a StreamingAssets file, or a direct video URL.", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(target)))
        {
            if (GUILayout.Button("Create Document Viewers"))
                CreateViewers((DocumentsPanel)target);
        }
    }

    private static void CreateViewers(DocumentsPanel menu)
    {
        Undo.RecordObject(menu, "Create document viewers");
        bool createPDF = menu.pdfViewer == null;
        bool createVideo = menu.videoViewer == null;
        menu.CreateViewers();
        if (createPDF)
            Undo.RegisterCreatedObjectUndo(menu.pdfViewer.gameObject, "Create PDF viewer");
        if (createVideo)
            Undo.RegisterCreatedObjectUndo(menu.videoViewer.gameObject, "Create video viewer");
        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
    }
}
