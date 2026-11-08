using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;
// =====================================================================================
// DocumentsPanel - opens page-image documents and playable videos beside the HUD.
//
// Shows a grid of documents (manuals, diagrams, videos). Pressing one opens it.
//
// WHERE THE DOCUMENTS COME FROM
//   * If "Document Data File" has a JSON file in it  -> one button is created per document in the file.
//   * If it is empty (or the file can't be read)     -> the buttons already in the prefab are used
//     (Document 1, 2, 3), with the names from "Default Document Names".
//   Other code can also call SetDocuments(...) at any time to load new data (e.g. from a server later).
//
// HOW TO SET UP
//   1. Open Assets/Prefabs/UI/DocumentsPanel.prefab (double-click it).
//   2. Click the top object "DocumentsPanel" and use Add Component -> "Documents Panel".
//   3. Drag the objects listed below into the matching slots in the Inspector.
//   4. You do NOT need to set up "On Click ()" on the buttons - this script does it for you.
//      (If you already added On Click entries, remove them or each press will be logged twice.)
//
// PREFAB LAYOUT (what each variable points to)
//   DocumentsPanel           <- this script goes here
//   └─ Background            <- the dark panel behind everything
//      ├─ Title              <- titleText
//      └─ Content            <- contentArea (new document buttons are created in here)
//         ├─ Document 1      <- defaultDocumentButtons[0]
//         ├─ Document 2      <- defaultDocumentButtons[1]
//         └─ Document 3      <- defaultDocumentButtons[2]
// =====================================================================================
public class DocumentsPanel : MonoBehaviour
{
    [Header("Objects in the prefab (drag these in from the Hierarchy)")]

    // The "Title" text object at the top of the panel (drag Background/Title here).
    public TMP_Text titleText;

    // The "Content" object that holds the document buttons (drag Background/Content here).
    public Transform contentArea;

    // The document buttons that are already in the prefab (drag Document 1, 2, 3 here, in order).
    // These are shown when there is no data.
    // ([FormerlySerializedAs] keeps what you already dragged in when this was called "documentButtons".)
    [FormerlySerializedAs("documentButtons")]
    public Button[] defaultDocumentButtons;

    [Header("Default content (used when there is no data)")]

    // Title shown at the top of the panel.
    public string title = "Documents";

    // Names put on the default document buttons. Leave the list empty to keep the text typed into the prefab.
    [FormerlySerializedAs("documentNames")]
    public string[] defaultDocumentNames = { "Document 1", "Document 2", "Document 3" };

    [Header("Data (optional)")]

    // A JSON file with the documents (see Assets/Data/ExampleDocuments.json). Leave empty for the demo.
    public TextAsset documentDataFile;

    // The button that gets copied for each document when data is loaded.
    // Leave empty to copy the first default button (Document 1).
    public Button documentButtonTemplate;

    [Header("Document viewers (created automatically if empty)")]
    public PDFViewerPanel pdfViewer;
    public DocumentVideoPanel videoViewer;

    [Header("Demo media")]
    [Tooltip("PDF pages converted to PNGs and imported as Sprite (2D and UI), in page order.")]
    public Sprite[] pdfPages;
    [Tooltip("Optional imported video clip. Takes priority over the JSON URL.")]
    public VideoClip documentVideoClip;
    [Tooltip("Optional path relative to Assets/StreamingAssets, e.g. PaperAirplane/basic-dart.mp4.")]
    public string videoStreamingAssetsPath;

    // Kept for existing scene references. Document playback uses videoViewer.
    [Header("Legacy popup (optional)")]
    public GameObject videoPopup;
    public VideoPlayer popupVideoPlayer;

    // The document buttons currently on screen (either the default ones or ones made from data).
    private List<Button> documentButtons = new List<Button>();

    // The info for each document currently on screen (name, type, url).
    private List<DocumentData> documents = new List<DocumentData>();

    private bool isViewerOpen;

    // Start runs once when the scene begins.
    void Start()
    {
        titleText.text = title;

        DocumentListData data = DocumentListData.FromJson(documentDataFile);

        if (data != null)
        {
            SetDocuments(data);
        }
        else
        {
            ShowDefaultDocuments();
        }
    }

    // ---------------- Loading documents ----------------

    // Replaces the panel's documents with the ones in "data": one button is created per document.
    // Call this from other scripts whenever new document data arrives.
    public void SetDocuments(DocumentListData data)
    {
        if (data == null || data.documents == null || System.Array.Exists(data.documents, item => item == null))
        {
            Debug.LogWarning("Documents: document data must contain valid entries.", this);
            return;
        }
        if (isViewerOpen)
            CloseDocument();
        Button template = documentButtonTemplate != null ? documentButtonTemplate : FirstDefaultButton();
        if (template == null)
        {
            Debug.LogWarning("Documents: no document button to copy. Fill in 'Document Button Template' or 'Default Document Buttons'.");
            return;
        }

        RemoveCreatedButtons();
        HideDefaultButtons();

        for (int i = 0; i < data.documents.Length; i++)
        {
            // Make a copy of the template inside "Content". The grid on Content lines it up automatically.
            Button newButton = Instantiate(template, contentArea);
            newButton.name = "Document " + (i + 1);
            newButton.gameObject.SetActive(true);

            // Clear any clicks copied from the template, so each new button only does its own job.
            newButton.onClick = new Button.ButtonClickedEvent();

            AddDocumentButton(newButton, data.documents[i]);
        }

        isViewerOpen = false;
    }

    // Uses the buttons that are already in the prefab, with the default names.
    private void ShowDefaultDocuments()
    {
        RemoveCreatedButtons();

        for (int i = 0; i < defaultDocumentButtons.Length; i++)
        {
            Button button = defaultDocumentButtons[i];
            button.gameObject.SetActive(true);

            // Use the name from "Default Document Names" if there is one, otherwise keep the text in the prefab.
            DocumentData document = new DocumentData();
            document.type = i == 1 ? "video" : "pdf";
            if (i < defaultDocumentNames.Length)
            {
                document.name = defaultDocumentNames[i];
            }
            else
            {
                document.name = button.GetComponentInChildren<TMP_Text>().text;
            }

            AddDocumentButton(button, document);
        }

        isViewerOpen = false;
    }

    // Puts a button on the list, sets its text, and makes pressing it call OpenDocument.
    private void AddDocumentButton(Button button, DocumentData document)
    {
        int documentNumber = documentButtons.Count; // copy the number now, so each button remembers its own

        documentButtons.Add(button);
        documents.Add(document);

        button.GetComponentInChildren<TMP_Text>().text = document.name;
        button.onClick.AddListener(() => OpenDocument(documentNumber));
    }

    // ---------------- Opening and closing ----------------

    // Called when a document button is pressed. documentNumber is 0 for the first document, 1 for the second, ...
    public void OpenDocument(int documentNumber)
    {
        if (documentNumber < 0 || documentNumber >= documents.Count)
            return;
        DocumentData document = documents[documentNumber];
        string type = (document.type ?? "").Trim().ToLowerInvariant();
        if (type != "pdf" && type != "image" && type != "video")
        {
            Debug.LogWarning("Documents: unsupported document type: " + document.type, this);
            return;
        }
        CreateViewers();
        CloseViewers();
        isViewerOpen = true;
        if (type == "video")
        {
            DocumentViewerFactory.SynchronizeLayout(this, (RectTransform)videoViewer.transform);
            videoViewer.Open(this, document.name, documentVideoClip, videoStreamingAssetsPath, document.url);
        }
        else
        {
            DocumentViewerFactory.SynchronizeLayout(this, (RectTransform)pdfViewer.transform);
            pdfViewer.Open(this, document.name, pdfPages);
        }
        // Viewers are siblings, so disabling the menu does not disable playback or Close.
        gameObject.SetActive(false);
    }

    // Close the open document and go back to the grid.
    public void CloseDocument()
    {
        isViewerOpen = false;
        CloseViewers();
        gameObject.SetActive(true);
    }

    // Also available in the Inspector to create editable panels before entering Play mode.
    public void CreateViewers()
    {
        if (pdfViewer == null)
            pdfViewer = DocumentViewerFactory.CreatePDF(this);
        if (videoViewer == null)
            videoViewer = DocumentViewerFactory.CreateVideo(this);
    }

    private void CloseViewers()
    {
        if (pdfViewer != null)
            pdfViewer.gameObject.SetActive(false);
        if (videoViewer != null)
            videoViewer.gameObject.SetActive(false);
        if (popupVideoPlayer != null)
            popupVideoPlayer.Stop();
        if (videoPopup != null)
            videoPopup.SetActive(false);
    }

    // Show the whole panel.
    public void Show()
    {
        CloseDocument();
    }

    // Hide the whole panel.
    public void Hide()
    {
        CloseViewers();
        isViewerOpen = false;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Runtime-created sibling panels must not outlive their menu.
        if (!Application.isPlaying)
            return;
        if (pdfViewer != null)
            Destroy(pdfViewer.gameObject);
        if (videoViewer != null)
            Destroy(videoViewer.gameObject);
    }

    // ---------------- Helpers ----------------

    // Deletes buttons that were created from data (the default buttons are kept).
    private void RemoveCreatedButtons()
    {
        foreach (Button button in documentButtons)
        {
            bool isDefaultButton = System.Array.IndexOf(defaultDocumentButtons, button) >= 0;
            if (!isDefaultButton)
            {
                Destroy(button.gameObject);
            }
            else
            {
                button.onClick.RemoveAllListeners(); // forget the old click so it isn't added twice
            }
        }
        documentButtons.Clear();
        documents.Clear();
    }

    // Hides the buttons that are already in the prefab (used when documents come from data instead).
    private void HideDefaultButtons()
    {
        foreach (Button button in defaultDocumentButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    // The first default button, or null if there are none.
    private Button FirstDefaultButton()
    {
        if (defaultDocumentButtons.Length > 0)
        {
            return defaultDocumentButtons[0];
        }
        return null;
    }
}
