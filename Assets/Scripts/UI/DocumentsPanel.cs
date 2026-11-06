using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================================
// DocumentsPanel  (STUB - attached to the RIGHT hand)
//
// Shows a grid of documents (manuals, diagrams, videos). Pressing one opens it.
//
// HOW TO SET UP
//   1. Open Assets/Prefabs/UI/DocumentsPanel.prefab (double-click it).
//   2. Click the top object "DocumentsPanel" and use Add Component -> "Documents Panel".
//   3. Drag the objects listed below into the matching slots in the Inspector.
//   4. For each document button: select it, find "On Click ()" in the Inspector, press "+",
//      drag the "DocumentsPanel" object into the slot, pick DocumentsPanel -> OpenDocument,
//      and type the button's number (0 for Document 1, 1 for Document 2, 2 for Document 3).
//
// PREFAB LAYOUT (what each variable points to)
//   DocumentsPanel           <- this script goes here
//   └─ Background            <- the dark panel behind everything
//      ├─ Title              <- titleText
//      └─ Content            <- the grid that holds the document buttons
//         ├─ Document 1      <- documentButtons[0]
//         ├─ Document 2      <- documentButtons[1]
//         └─ Document 3      <- documentButtons[2]
// =====================================================================================
public class DocumentsPanel : MonoBehaviour
{
    // The "Title" text object at the top of the panel (drag Background/Title here).
    public TMP_Text titleText;

    // The document buttons inside "Content" (drag Document 1, 2, 3 here, in order).
    public Button[] documentButtons;

    // The name shown on each document button. Edit these in the Inspector.
    public string[] documentNames = { "Document 1", "Document 2", "Document 3" };

    // Which document is open right now (-1 = none open, the grid is showing).
    private int openDocument = -1;

    // Start runs once when the scene begins.
    void Start()
    {
        titleText.text = "Documents";
        UpdateDocumentButtons();
    }

    // Called by a document button's "On Click ()" event. documentNumber is 0 for Document 1, ...
    public void OpenDocument(int documentNumber)
    {
        openDocument = documentNumber;
        Debug.Log("Documents: opened " + documentNames[documentNumber]);
        // TODO: hide the grid and show the document (PDF page, image, video, or 3D model).
    }

    // Close the open document and go back to the grid.
    public void CloseDocument()
    {
        Debug.Log("Documents: closed document " + openDocument);
        openDocument = -1;
        // TODO: hide the document and show the grid again.
    }

    // Show the whole panel.
    public void Show()
    {
        gameObject.SetActive(true);
    }

    // Hide the whole panel.
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Copies the names from "documentNames" onto the document buttons.
    private void UpdateDocumentButtons()
    {
        for (int i = 0; i < documentButtons.Length && i < documentNames.Length; i++)
        {
            // Each button has a child "Label" text object; this finds it and changes its words.
            TMP_Text label = documentButtons[i].GetComponentInChildren<TMP_Text>();
            label.text = documentNames[i];
        }
    }
}
