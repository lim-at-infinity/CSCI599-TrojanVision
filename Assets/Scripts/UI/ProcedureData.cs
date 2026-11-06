using UnityEngine;

// =====================================================================================
// ProcedureData - the "incoming data" for the Procedures panel.
//
// These classes are NOT attached to GameObjects. They just describe the shape of the data,
// so Unity can read it from a JSON file like this:
//
//   {
//     "title": "Front Brake Pad Replacement",
//     "steps": [
//       { "text": "Loosen lug nuts" },
//       { "text": "Lift the car and set a jack stand" }
//     ]
//   }
//
// See Assets/Data/ExampleProcedure.json for a full example.
// =====================================================================================

// One whole procedure: a title and a list of steps.
[System.Serializable]
public class ProcedureData
{
    public string title;      // shown in the panel's Title text
    public StepData[] steps;  // one entry per step button

    // Reads a JSON file into a ProcedureData.
    // Returns null if there is no file, or the file is broken / has no steps.
    // (null means "no data" - the panel then uses its default content instead.)
    public static ProcedureData FromJson(TextAsset jsonFile)
    {
        if (jsonFile == null)
        {
            return null;
        }

        try
        {
            ProcedureData data = JsonUtility.FromJson<ProcedureData>(jsonFile.text);
            if (data == null || data.steps == null || data.steps.Length == 0)
            {
                Debug.LogWarning("ProcedureData: '" + jsonFile.name + "' has no steps. Using default content.");
                return null;
            }
            return data;
        }
        catch (System.Exception error)
        {
            Debug.LogWarning("ProcedureData: could not read '" + jsonFile.name + "': " + error.Message + ". Using default content.");
            return null;
        }
    }
}

// One step in a procedure.
[System.Serializable]
public class StepData
{
    public string text;  // the words shown on the step button
}
