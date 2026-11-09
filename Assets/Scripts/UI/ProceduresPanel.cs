using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// =====================================================================================
// ProceduresPanel  (STUB - attached to the LEFT hand)
//
// Shows the steps of a task (e.g. "Step 1: Loosen lug nuts") so the user can follow along.
//
// WHERE THE STEPS COME FROM
//   * If "Procedure Data File" has a JSON file in it  -> one button is created per step in the file.
//   * If it is empty (or the file can't be read)      -> the buttons already in the prefab are used
//     (Step 1, Step 2, Step 3), with the text from "Default Steps".
//   Other code can also call SetSteps(...) at any time to load new data (e.g. from a server later).
//
// HOW TO SET UP
//   1. Open Assets/Prefabs/UI/ProceduresPanel.prefab (double-click it).
//   2. Click the top object "ProceduresPanel" and use Add Component -> "Procedures Panel".
//   3. Drag the objects listed below into the matching slots in the Inspector.
//   4. You do NOT need to set up "On Click ()" on the buttons - this script does it for you.
//      (If you already added On Click entries, remove them or each press will be logged twice.)
//
// PREFAB LAYOUT (what each variable points to)
//   ProceduresPanel          <- this script goes here
//   └─ Background            <- the dark panel behind everything
//      ├─ Title              <- titleText
//      └─ Content            <- contentArea (new step buttons are created in here)
//         ├─ Step 1          <- defaultStepButtons[0]
//         ├─ Step 2          <- defaultStepButtons[1]
//         └─ Step 3          <- defaultStepButtons[2]
// =====================================================================================
public class ProceduresPanel : MonoBehaviour
{
    [Header("Objects in the prefab (drag these in from the Hierarchy)")]

    // The "Title" text object at the top of the panel (drag Background/Title here).
    public TMP_Text titleText;

    // The "Content" object that holds the step buttons (drag Background/Content here).
    public Transform contentArea;

    // The step buttons that are already in the prefab (drag Step 1, Step 2, 3 here, in order).
    // These are shown when there is no data.
    // ([FormerlySerializedAs] keeps what you already dragged in when this was called "stepButtons".)
    [FormerlySerializedAs("stepButtons")]
    public Button[] defaultStepButtons;

    [Header("Default content (used when there is no data)")]

    // Title shown when there is no data.
    public string defaultTitle = "Procedures";

    // Text put on the default step buttons. Leave the list empty to keep the text typed into the prefab.
    [FormerlySerializedAs("steps")]
    public string[] defaultSteps = { "Step 1 - placeholder", "Step 2 - placeholder", "Step 3 - placeholder" };

    [Header("Data (optional)")]

    // A JSON file with the steps (see Assets/Data/ExampleProcedure.json). Leave empty for the demo.
    public TextAsset procedureDataFile;

    // The button that gets copied for each step when data is loaded.
    // Leave empty to copy the first default button (Step 1).
    public Button stepButtonTemplate;

    // The step buttons currently on screen (either the default ones or ones made from data).
    private List<Button> stepButtons = new List<Button>();

    // The text of each step currently on screen.
    private List<string> stepTexts = new List<string>();

    // Which step is selected (-1 = none selected, 0 = the first step).
    private int currentStep = -1;

    public TMP_Text stepCounterText;

    // NEW: Each button has its own independent on/off state.
    private List<bool> completedSteps = new List<bool>();

    // NEW: Remember each button's original appearance.
    private List<ColorBlock> originalColors = new List<ColorBlock>();
    private List<Color> originalGraphicColors = new List<Color>();
    private List<Selectable.Transition> originalTransitions =
        new List<Selectable.Transition>();

    // Start runs once when the scene begins.
    void Start()
    {
        ProcedureData data = ProcedureData.FromJson(procedureDataFile);

        if (data != null)
        {
            SetSteps(data);
        }
        else
        {
            ShowDefaultSteps();
        }
    }

    // ---------------- Loading steps ----------------

    // Replaces the panel's steps with the ones in "data": one button is created per step.
    // Call this from other scripts whenever new step data arrives.
    public void SetSteps(ProcedureData data)
    {
        Button template = stepButtonTemplate != null ? stepButtonTemplate : FirstDefaultButton();
        if (template == null)
        {
            Debug.LogWarning("Procedures: no step button to copy. Fill in 'Step Button Template' or 'Default Step Buttons'.");
            return;
        }

        RemoveCreatedButtons();
        HideDefaultButtons();

        titleText.text = string.IsNullOrEmpty(data.title) ? defaultTitle : data.title;

        for (int i = 0; i < data.steps.Length; i++)
        {
            // Make a copy of the template inside "Content". The layout on Content lines it up automatically.
            Button newButton = Instantiate(template, contentArea);
            newButton.name = "Step " + (i + 1);
            newButton.gameObject.SetActive(true);

            // Clear any clicks copied from the template, so each new button only does its own job.
            newButton.onClick = new Button.ButtonClickedEvent();

            AddStepButton(newButton, data.steps[i].text);
        }

        ApplyHandPanelLayout();
        currentStep = -1;
        UpdateStepButtons();
    }

    // Uses the buttons that are already in the prefab, with the default text.
    private void ShowDefaultSteps()
    {
        RemoveCreatedButtons();
        titleText.text = defaultTitle;

        for (int i = 0; i < defaultStepButtons.Length; i++)
        {
            Button button = defaultStepButtons[i];
            button.gameObject.SetActive(true);

            // Use the text from "Default Steps" if there is one, otherwise keep the text in the prefab.
            string text;
            if (i < defaultSteps.Length)
            {
                text = defaultSteps[i];
            }
            else
            {
                text = button.GetComponentInChildren<TMP_Text>().text;
            }

            AddStepButton(button, text);
        }

        ApplyHandPanelLayout();
        currentStep = -1;
        UpdateStepButtons();
    }

    // Places the step counter just under the panel, and sizes the title to fit on one line.
    // Only for hand-attached panels (XR-Setup). The TrojanVisionDemo scene positions its own counter.
    private void ApplyHandPanelLayout()
    {
        if (GetComponent<HandFollowPanel>() == null)
        {
            return;
        }

        titleText.fontSize = 32f;

        if (stepCounterText != null)
        {
            stepCounterText.fontSize = 30f;

            RectTransform counter = stepCounterText.rectTransform;
            counter.anchorMin = new Vector2(0f, 0f);
            counter.anchorMax = new Vector2(1f, 0f);
            counter.pivot = new Vector2(0.5f, 1f);
            counter.anchoredPosition = new Vector2(0f, -6f);
            counter.sizeDelta = new Vector2(-40f, 50f);
        }
    }

    // Shrinks long step text so it stays inside its button. Only for hand-attached panels (XR-Setup).
    private void FitLabelInButton(TMP_Text label)
    {
        if (GetComponent<HandFollowPanel>() == null)
        {
            return;
        }

        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 26f;
    }

    // Puts a button on the list, sets its text, and makes pressing it call OnStepButtonPressed.
    private void AddStepButton(Button button, string text)
    {
        int stepNumber = stepButtons.Count; // copy the number now, so each button remembers its own

        stepButtons.Add(button);
        stepTexts.Add(text);

        // NEW: Start off and remember the original appearance.
        completedSteps.Add(false);
        originalColors.Add(button.colors);
        originalTransitions.Add(button.transition);
        originalGraphicColors.Add(
            button.targetGraphic != null
                ? button.targetGraphic.color
                : Color.white
        );

        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.text = text;
        FitLabelInButton(label);
        button.onClick.AddListener(() => OnStepButtonPressed(stepNumber));
    }

    // ---------------- Button presses and navigation ----------------

    // Called when a step button is pressed. stepNumber is 0 for the first step, 1 for the second, ...
    public void OnStepButtonPressed(int stepNumber)
    {
        if (stepNumber < 0 || stepNumber >= stepButtons.Count)
        {
            return;
        }

        // NEW: Toggle only the clicked button.
        completedSteps[stepNumber] = !completedSteps[stepNumber];
        currentStep = stepNumber;

        UpdateStepButtons();
    }

    // Move to the next step.
    public void NextStep()
    {
        if (currentStep < stepButtons.Count - 1)
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

    // ---------------- Helpers ----------------

    // Redraws the step buttons to show which step is current.
    // NEW: Button color now follows its independent on/off state.
    private void UpdateStepButtons()
    {
        int completedCount = 0;

        for (int i = 0; i < stepButtons.Count; i++)
        {
            Button button = stepButtons[i];

            if (completedSteps[i])
            {
                completedCount++;

                // NEW: Blue stays blue even while hovered or selected.
                Color blue = new Color(0.1f, 0.5f, 1f);

                button.transition = Selectable.Transition.ColorTint;

                if (button.targetGraphic != null)
                {
                    button.targetGraphic.color = Color.white;
                }

                ColorBlock colors = originalColors[i];
                colors.normalColor = blue;
                colors.highlightedColor = blue;
                colors.pressedColor = blue;
                colors.selectedColor = blue;
                colors.disabledColor = blue;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0f;

                button.colors = colors;

                if (button.targetGraphic != null)
                {
                    button.targetGraphic.CrossFadeColor(
                        blue, 0f, true, true
                    );
                }
            }
            else
            {
                // NEW: Restore the button's actual original appearance.
                RestoreButtonAppearance(i);
            }
        }

        if (stepCounterText != null)
        {
            stepCounterText.text = stepButtons.Count > 0 && completedCount == stepButtons.Count
                ? "Procedure complete"
                : $"{completedCount} of {stepButtons.Count} steps complete";
        }
    }

    // NEW: Restore saved colors instead of using a fixed red or gray.
    private void RestoreButtonAppearance(int index)
    {
        Button button = stepButtons[index];

        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = originalGraphicColors[index];
        }

        button.colors = originalColors[index];
        button.transition = originalTransitions[index];

        if (button.targetGraphic != null &&
            button.transition == Selectable.Transition.ColorTint)
        {
            button.targetGraphic.CrossFadeColor(
                originalColors[index].normalColor *
                originalColors[index].colorMultiplier,
                0f,
                true,
                true
            );
        }
    }

    // Deletes buttons that were created from data (the default buttons are kept).
    private void RemoveCreatedButtons()
    {
        // NEW: Restore originals before reusing them as templates.
        for (int i = 0; i < stepButtons.Count; i++)
        {
            RestoreButtonAppearance(i);
        }

        foreach (Button button in stepButtons)
        {
            bool isDefaultButton = System.Array.IndexOf(defaultStepButtons, button) >= 0;
            if (!isDefaultButton)
            {
                Destroy(button.gameObject);
            }
            else
            {
                button.onClick.RemoveAllListeners(); // forget the old click so it isn't added twice
            }
        }
        stepButtons.Clear();
        stepTexts.Clear();

        // NEW: Clear states and saved appearance when loading a new list.
        completedSteps.Clear();
        originalColors.Clear();
        originalGraphicColors.Clear();
        originalTransitions.Clear();
    }

    // Hides the buttons that are already in the prefab (used when steps come from data instead).
    private void HideDefaultButtons()
    {
        foreach (Button button in defaultStepButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    // The first default button, or null if there are none.
    private Button FirstDefaultButton()
    {
        if (defaultStepButtons.Length > 0)
        {
            return defaultStepButtons[0];
        }
        return null;
    }
}