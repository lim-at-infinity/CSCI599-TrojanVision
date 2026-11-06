using UnityEngine;

// =====================================================================================
// DocumentData - the "incoming data" for the Documents panel.
//
// These classes are NOT attached to GameObjects. They just describe the shape of the data,
// so Unity can read it from a JSON file like this:
//
//   {
//     "documents": [
//       { "name": "Brake Service Manual", "type": "pdf",   "url": "" },
//       { "name": "Caliper Diagram",      "type": "image", "url": "" }
//     ]
//   }
//
// See Assets/Data/ExampleDocuments.json for a full example.
// =====================================================================================

// The whole list of documents.
[System.Serializable]
public class DocumentListData
{
    public DocumentData[] documents;  // one entry per document button

    // Reads a JSON file into a DocumentListData.
    // Returns null if there is no file, or the file is broken / has no documents.
    // (null means "no data" - the panel then uses its default content instead.)
    public static DocumentListData FromJson(TextAsset jsonFile)
    {
        if (jsonFile == null)
        {
            return null;
        }

        try
        {
            DocumentListData data = JsonUtility.FromJson<DocumentListData>(jsonFile.text);
            if (data == null || data.documents == null || data.documents.Length == 0)
            {
                Debug.LogWarning("DocumentData: '" + jsonFile.name + "' has no documents. Using default content.");
                return null;
            }
            return data;
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("DocumentData: could not read '" + jsonFile.name + "': " + error.Message + ". Using default content.");
            return null;
        }
    }
}

// One document.
[System.Serializable]
public class DocumentData
{
    public string name;  // the words shown on the document button
    public string type;  // what kind of file: "pdf", "image", "video" or "model"
    public string url;   // where the file lives (e.g. a Supabase link). Not used yet.
}
