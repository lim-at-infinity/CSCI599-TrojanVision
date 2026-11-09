using System.Text.RegularExpressions;
using Oculus.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

// =====================================================================================
// VoiceCommands - control the panels by speaking.
//
// The app keeps listening for "Hey Trojan". Saying it shows "Mic listening", and the next thing
// the user says is treated as a command. A mic icon and feedback text appear at the lower middle of the
// screen only while the app is responding, and stay fixed to the view while they are showing.
// Wit.ai turns the speech into text, and this script looks for key words in that text.
// While the app is open, microphone audio is streamed to Wit.ai so it can hear "Hey Trojan".
// Things the user can say after "Hey Trojan":
//   "Mark step 3"          "Open the video" / "Open the instructions"       "Close"
//   "Play" / "Pause"       "Next page" / "Previous page"       "Forward" / "Back"
//
// HOW TO SET UP
//   1. Assets -> Create -> Voice SDK -> Add App Voice Experience to Scene, and assign your Wit Configuration.
//   2. Add Component -> "Voice Commands" on that same object.
// =====================================================================================
[RequireComponent(typeof(AppVoiceExperience))]
public class VoiceCommands : MonoBehaviour
{
    private static readonly Color IdleColor = new Color(0.12f, 0.25f, 0.37f);
    private static readonly Color ListeningColor = new Color(0.8f, 0.1f, 0.1f);

    // Seconds to wait before listening again, normally and after an error.
    private const float RestartDelay = 0.3f;
    private const float ErrorRetryDelay = 5f;

    // Matches "Trojan" and spellings speech recognition may produce instead, like "Trogen" or "Trojans".
    private static readonly Regex WakeWord = new Regex(@"\btro[jg][ae]n('?s)?\b", RegexOptions.IgnoreCase);

    // Number words, including ones speech recognition often hears instead ("to" for "two").
    private static readonly string[][] NumberWords =
    {
        new[] { "one", "won" },
        new[] { "two", "to", "too" },
        new[] { "three", "tree" },
        new[] { "four", "for" },
        new[] { "five" },
        new[] { "six" },
        new[] { "seven" },
        new[] { "eight", "ate" },
        new[] { "nine" },
        new[] { "ten" }
    };

    // The panels to control. Leave empty to find them automatically.
    public ProceduresPanel procedures;
    public DocumentsPanel documents;

    [Header("Hey Trojan")]

    // Listen for "Hey Trojan" the whole time the app is open. Untick to turn voice off.
    public bool listenForHeyTrojan = true;

    // How long the app waits for a command after "Hey Trojan", in seconds.
    public float commandWindowSeconds = 8f;

    [Header("Mic icon and feedback text")]

    // How far from the eyes they appear, in meters.
    public float hudDistance = 0.9f;

    // How far below the middle of the view they sit, in degrees.
    public float hudAngleBelowView = 18f;

    // How long feedback like "Heard: close" stays on screen, in seconds.
    public float feedbackSeconds = 3f;

    // Meta's voice component on this object.
    private AppVoiceExperience voice;

    // True after "Hey Trojan", while the app is waiting for a command.
    private bool isWaitingForCommand = false;

    // The time the app stops waiting for a command.
    private float commandWindowEnd = 0f;

    // The earliest time the app may start listening again.
    private float nextListenTime = 0f;

    // The Canvas fixed to the user's view that holds the mic icon and feedback text, both created by this script.
    private RectTransform hud;
    private Image micButtonImage;
    private TMP_Text statusText;

    // The time the feedback text is hidden again.
    private float feedbackEndTime = 0f;

    // True after a voice error has been shown, so a lost connection doesn't keep popping up.
    private bool hasShownError = false;

    void Awake()
    {
        voice = GetComponent<AppVoiceExperience>();

        if (procedures == null)
        {
            procedures = FindFirstObjectByType<ProceduresPanel>();
        }

        if (documents == null)
        {
            documents = FindFirstObjectByType<DocumentsPanel>();
        }
    }

    void Start()
    {
        if (documents == null)
        {
            Debug.LogWarning("VoiceCommands: no Documents panel found, so there is no Mic button.");
            return;
        }

        CreateHud();
        if (hud == null)
        {
            return;
        }

        CreateMicButton();
        CreateStatusText();
        hud.gameObject.SetActive(false);

        if (!IsConfigured())
        {
            Debug.LogWarning("VoiceCommands: assign a Wit Configuration on App Voice Experience.");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    // Stops waiting for a command when time runs out, and keeps the microphone listening.
    void Update()
    {
        if (isWaitingForCommand && Time.time >= commandWindowEnd)
        {
            StopWaitingForCommand();
            feedbackEndTime = 0f;
        }

        // Nothing stays on screen unless the app is responding to the user.
        if (hud != null && hud.gameObject.activeSelf && !isWaitingForCommand && Time.time >= feedbackEndTime)
        {
            hud.gameObject.SetActive(false);
        }

        if (!listenForHeyTrojan || Time.time < nextListenTime || voice.Active || voice.IsRequestActive)
        {
            return;
        }

        if (IsConfigured() && HasMicrophonePermission())
        {
            nextListenTime = Time.time + RestartDelay;
            voice.Activate();
        }
    }

    void OnEnable()
    {
        voice.VoiceEvents.OnFullTranscription.AddListener(OnHeardText);
        voice.VoiceEvents.OnError.AddListener(OnVoiceError);
    }

    void OnDisable()
    {
        voice.VoiceEvents.OnFullTranscription.RemoveListener(OnHeardText);
        voice.VoiceEvents.OnError.RemoveListener(OnVoiceError);
    }

    // ---------------- Listening ----------------

    private bool IsConfigured()
    {
        return voice.RuntimeConfiguration != null && voice.RuntimeConfiguration.witConfiguration != null;
    }

    private bool HasMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return true;
#endif
    }

    private void StartWaitingForCommand()
    {
        isWaitingForCommand = true;
        commandWindowEnd = Time.time + commandWindowSeconds;
        SetMicColor(ListeningColor);
        ShowStatus("Mic listening");

        if (hud != null)
        {
            hud.gameObject.SetActive(true);
        }
    }

    private void StopWaitingForCommand()
    {
        isWaitingForCommand = false;
        SetMicColor(IdleColor);
    }

    private void OnVoiceError(string error, string message)
    {
        // Don't retry straight away, or a network problem would repeat many times a second.
        nextListenTime = Time.time + ErrorRetryDelay;
        Debug.LogWarning("VoiceCommands: " + error + " - " + message);

        // Only tell the user once, or a retry every few seconds would keep interrupting them.
        bool shouldShow = isWaitingForCommand || !hasShownError;
        StopWaitingForCommand();

        if (shouldShow)
        {
            hasShownError = true;
            ShowFeedback("Voice error - check Wi-Fi");
        }
    }

    // Shows a short message for a few seconds.
    private void ShowFeedback(string message)
    {
        ShowStatus(message);
        feedbackEndTime = Time.time + feedbackSeconds;

        if (hud != null)
        {
            hud.gameObject.SetActive(true);
        }
    }

    // Wit.ai has finished turning the user's speech into text.
    private void OnHeardText(string text)
    {
        Debug.Log("VoiceCommands: heard '" + text + "'");
        hasShownError = false;

        string command = text;
        Match wakeWord = WakeWord.Match(text);

        if (wakeWord.Success)
        {
            // Anything said after "Hey Trojan" in the same breath is the command.
            command = text.Substring(wakeWord.Index + wakeWord.Length).Trim(' ', ',', '.', '!', '?');

            if (command.Length == 0)
            {
                StartWaitingForCommand();
                return;
            }
        }
        else if (!isWaitingForCommand)
        {
            // Ordinary speech, not meant for the app.
            return;
        }

        if (RunCommand(command.ToLowerInvariant()))
        {
            StopWaitingForCommand();
            ShowFeedback("Heard: " + command);
        }
        else
        {
            StartWaitingForCommand();
            ShowStatus("No command in: " + command);
        }
    }

    // ---------------- Commands ----------------

    // Looks for key words and does the matching action. Returns false if nothing matched.
    private bool RunCommand(string said)
    {
        if (TryMarkStep(said))
        {
            return true;
        }

        if (said.Contains("open"))
        {
            documents.OpenFirstDocumentOfType(said.Contains("video") ? "video" : "pdf");
            return true;
        }

        if (said.Contains("close") || said.Contains("exit"))
        {
            documents.CloseDocument();
            return true;
        }

        if (said.Contains("page"))
        {
            return TurnPage(said);
        }

        if (said.Contains("pause") || said.Contains("stop"))
        {
            return SetVideoPlaying(false);
        }

        if (said.Contains("play") || said.Contains("resume"))
        {
            return SetVideoPlaying(true);
        }

        if (said.Contains("forward"))
        {
            return SkipVideo(true);
        }

        if (said.Contains("back") || said.Contains("rewind"))
        {
            return SkipVideo(false);
        }

        return false;
    }

    // Handles phrases like "mark step 3 done" or "step three".
    private bool TryMarkStep(string said)
    {
        Match match = Regex.Match(said, @"step\s+(\w+)");
        if (!match.Success || procedures == null)
        {
            return false;
        }

        int stepNumber = ParseNumber(match.Groups[1].Value);
        if (stepNumber < 1)
        {
            return false;
        }

        procedures.OnStepButtonPressed(stepNumber - 1);
        return true;
    }

    // Turns "3" or "three" into 3. Returns 0 if the word is not a number.
    private int ParseNumber(string word)
    {
        if (int.TryParse(word, out int number))
        {
            return number;
        }

        for (int i = 0; i < NumberWords.Length; i++)
        {
            if (System.Array.IndexOf(NumberWords[i], word) >= 0)
            {
                return i + 1;
            }
        }

        return 0;
    }

    private bool TurnPage(string said)
    {
        PDFViewerPanel pdf = documents.pdfViewer;
        if (pdf == null || !pdf.isActiveAndEnabled)
        {
            return false;
        }

        if (said.Contains("next"))
        {
            pdf.NextPage();
            return true;
        }

        if (said.Contains("previous") || said.Contains("back") || said.Contains("last"))
        {
            pdf.PreviousPage();
            return true;
        }

        return false;
    }

    private bool SetVideoPlaying(bool shouldPlay)
    {
        DocumentVideoPanel video = documents.videoViewer;
        if (video == null || !video.isActiveAndEnabled)
        {
            return false;
        }

        // TogglePlayback flips the state, so only call it when the video is in the other state.
        if (video.player.isPlaying != shouldPlay)
        {
            video.TogglePlayback();
        }

        return true;
    }

    private bool SkipVideo(bool forward)
    {
        DocumentVideoPanel video = documents.videoViewer;
        if (video == null || !video.isActiveAndEnabled)
        {
            return false;
        }

        if (forward)
        {
            video.ForwardTenSeconds();
        }
        else
        {
            video.BackwardTenSeconds();
        }

        return true;
    }

    // ---------------- On-screen controls ----------------

    // Builds a Canvas fixed to the lower middle of the user's view.
    private void CreateHud()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("VoiceCommands: no Main Camera found, so the Mic button can't be placed.");
            return;
        }

        hud = new GameObject("VoiceHud", typeof(RectTransform)).GetComponent<RectTransform>();
        hud.gameObject.layer = documents.gameObject.layer;

        // A child of the camera moves with the head, so it stays in the same spot on screen.
        hud.SetParent(Camera.main.transform, false);
        hud.sizeDelta = new Vector2(500f, 140f);
        hud.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f);

        float angle = hudAngleBelowView * Mathf.Deg2Rad;
        hud.localPosition = new Vector3(0f, -Mathf.Sin(angle), Mathf.Cos(angle)) * hudDistance;
        hud.localRotation = Quaternion.LookRotation(hud.localPosition, Vector3.up);

        Canvas canvas = hud.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;
    }

    // Adds the mic icon at the top of the Canvas. It only shows status: it moves with the head, so it can't be pressed.
    private void CreateMicButton()
    {
        RectTransform rect = CreateRect("MicButton");
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(120f, 70f);

        micButtonImage = rect.gameObject.AddComponent<Image>();
        micButtonImage.color = IdleColor;
        micButtonImage.raycastTarget = false;

        TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.gameObject.layer = rect.gameObject.layer;
        label.rectTransform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        StyleText(label, "Mic", 24f);
        label.color = Color.white;
    }

    // Adds a line of text under the Mic button.
    private void CreateStatusText()
    {
        RectTransform rect = CreateRect("VoiceStatus");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.right;
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 60f);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(text, "", 30f);
        statusText = text;
    }

    private RectTransform CreateRect(string objectName)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(hud, false);
        rect.gameObject.layer = hud.gameObject.layer;
        return rect;
    }

    private void StyleText(TMP_Text text, string value, float size)
    {
        if (documents.titleText != null)
        {
            text.font = documents.titleText.font;
        }

        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.8f, 0f);
        text.raycastTarget = false;
        text.richText = false;
    }

    private void SetMicColor(Color color)
    {
        if (micButtonImage != null)
        {
            micButtonImage.color = color;
        }
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
