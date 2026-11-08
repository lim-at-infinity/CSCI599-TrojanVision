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
            {
                var menu = (DocumentsPanel)target;
                Undo.RecordObject(menu, "Create document viewers");
                var previousPDF = menu.pdfViewer;
                var previousVideo = menu.videoViewer;
                menu.CreateViewers();
                if (previousPDF == null)
                    Undo.RegisterCreatedObjectUndo(menu.pdfViewer.gameObject, "Create PDF viewer");
                if (previousVideo == null)
                    Undo.RegisterCreatedObjectUndo(menu.videoViewer.gameObject, "Create video viewer");
                PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
                EditorUtility.SetDirty(menu);
                EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
            }
        }
    }
}
