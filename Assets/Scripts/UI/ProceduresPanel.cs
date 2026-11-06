using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================================
// ProceduresPanel  (STUB - attached to the LEFT hand)
//
// Shows the steps of a task (e.g. "Step 1: Loosen lug nuts") so the user can follow along.
//
// HOW TO SET UP
//   1. Open Assets/Prefabs/UI/ProceduresPanel.prefab (double-click it).
//   2. Click the top object "ProceduresPanel" and use Add Component -> "Procedures Panel".
//   3. Drag the objects listed below into the matching slots in the Inspector.
//   4. For each step button: select it, find "On Click ()" in the Inspector, press "+",
//      drag the "ProceduresPanel" object into the slot, pick ProceduresPanel -> OnStepButtonPressed,
//      and type the button's number (0 for Step 1, 1 for Step 2, 2 for Step 3).
//
// PREFAB LAYOUT (what each variable points to)
//   ProceduresPanel          <- this script goes here
//   └─ Background            <- the dark panel behind everything
//      ├─ Title              <- titleText
//      └─ Content            <- the area that holds the step buttons
//         ├─ Step 1          <- stepButtons[0]
//         ├─ Step 2          <- stepButtons[1]
//         └─ Step 3          <- stepButtons[2]
// =====================================================================================
public class ProceduresPanel : MonoBehaviour
{
    // The "Title" text object at the top of the panel (drag Background/Title here).
    public TMP_Text titleText;

    // The step buttons inside "Content" (drag Step 1, Step 2, Step 3 here, in order).
    public Button[] stepButtons;

    // The text shown for each step. Edit these in the Inspector.
    public string[] steps = { "Step 1 - placeholder", "Step 2 - placeholder", "Step 3 - placeholder" };

    // Which step the user is on right now (0 = the first step).
    private int currentStep = 0;

    // Start runs once when the scene begins.
    void Start()
    {
        titleText.text = "Procedures";
        UpdateStepButtons();
    }

    // Called by a step button's "On Click ()" event. stepNumber is 0 for Step 1, 1 for Step 2, ...
    public void OnStepButtonPressed(int stepNumber)
    {
        Debug.Log("Procedures: step button " + stepNumber + " was pressed");
        // TODO: decide what pressing a step does (mark it complete? select it?).
    }

    // Move to the next step.
    public void NextStep()
    {
        if (currentStep < steps.Length - 1)
        {
            currentStep++;
        }
        Debug.Log("Procedures: now on step " + currentStep);
        UpdateStepButtons();
    }

    // Move back one step.
    public void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
        }
        Debug.Log("Procedures: now on step " + currentStep);
        UpdateStepButtons();
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

    // Copies the text from "steps" onto the step buttons.
    private void UpdateStepButtons()
    {
        for (int i = 0; i < stepButtons.Length && i < steps.Length; i++)
        {
            // Each button has a child "Label" text object; this finds it and changes its words.
            TMP_Text label = stepButtons[i].GetComponentInChildren<TMP_Text>();
            label.text = steps[i];
        }
        // TODO: highlight stepButtons[currentStep] and grey out finished steps.
    }
}
