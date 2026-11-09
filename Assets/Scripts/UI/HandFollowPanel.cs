using UnityEngine;

// =====================================================================================
// HandFollowPanel - makes a UI panel float above a hand.
//
//
// HOW TO SET UP
//   1. In the scene, select the panel (e.g. OVRCameraRig/TrackingSpace/LeftHandAnchor/ProceduresPanel).
//   2. Add Component -> "Hand Follow Panel".
//   3. Set "Hand Type" to Hand Left for the left-hand panel, Hand Right for the right-hand panel.
//   4. Untick "Turn To Face" on the same object - this script handles facing now, and the two would fight.
// =====================================================================================
public class HandFollowPanel : MonoBehaviour
{
    [Header("What to follow (leave empty to find automatically)")]

    // The hand to float above. Leave empty to use the object this panel sits under (LeftHandAnchor / RightHandAnchor).
    public Transform hand;

    // The user's head, which the panel turns to face. Leave empty to use the Main Camera (CenterEyeAnchor).
    public Transform head;

    // Which hand this is - used to check whether that hand is being tracked.
    public OVRPlugin.Hand handType = OVRPlugin.Hand.HandLeft;

    [Header("Placement")]

    // How far above the hand the panel floats, in meters (0.12 = 12 cm).
    public float heightAboveHand = 0.12f;

    // How quickly the panel catches up to the hand. Higher = snappier, lower = floatier.
    public float followSpeed = 10f;

    [Header("Hiding")]

    // Hide the panel while its hand is not tracked (e.g. hand out of view, or hand behind your back).
    public bool hideWhenHandLost = true;

    // The panel's Canvas - turned off to hide the panel without turning off this script.
    private Canvas panelCanvas;

    // Where the panel is drawn this frame (kept separately so smoothing isn't thrown off by the parent moving).
    private Vector3 currentPosition;
    private Quaternion currentRotation;

    // False until the first frame, so the panel jumps straight into place instead of sliding in from far away.
    private bool hasStarted = false;

    // False until the hand has been tracked once. Keeps the panel visible in the Editor without a headset.
    private bool hasSeenHand = false;

    // Runs every time the panel is shown again (e.g. after a document viewer closes).
    // Makes the next frame jump straight into place instead of sliding over from the old spot.
    void OnEnable()
    {
        hasStarted = false;
    }

    void Start()
    {
        if (hand == null)
        {
            hand = transform.parent;
        }

        if (head == null && Camera.main != null)
        {
            head = Camera.main.transform;
        }

        panelCanvas = GetComponent<Canvas>();

        if (hand == null || head == null)
        {
            Debug.LogWarning("HandFollowPanel on '" + name + "': could not find the hand or the head. Drag them into the Inspector.");
        }
    }

    // LateUpdate runs after the hands have been moved for this frame, so the panel never lags a frame behind.
    void LateUpdate()
    {
        if (hand == null || head == null)
        {
            return;
        }

        UpdateVisibility();

        // Target position: the hand's position, keeps panel upright
        Vector3 targetPosition = hand.position + Vector3.up * heightAboveHand;

        // Target rotation: face away from the head (a Canvas is read from behind its "forward"),
        // using only the left/right direction so the panel stays upright.
        Vector3 awayFromHead = targetPosition - head.position;
        awayFromHead.y = 0f;
        Quaternion targetRotation = currentRotation;
        if (awayFromHead.sqrMagnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(awayFromHead, Vector3.up);
        }

        if (!hasStarted)
        {
            // First frame: jump straight into place.
            currentPosition = targetPosition;
            currentRotation = targetRotation;
            hasStarted = true;
        }
        else
        {
            // Every other frame: move part of the way there (smooth, and the same speed at any frame rate).
            float amount = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, amount);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, amount);
        }

        transform.SetPositionAndRotation(currentPosition, currentRotation);
    }

    // Gives this panel the same hand, head and settings as another panel.
    // Used so the PDF and video viewers float above the same hand as the Documents menu.
    public void CopySettingsFrom(HandFollowPanel source)
    {
        hand = source.hand;
        head = source.head;
        handType = source.handType;
        heightAboveHand = source.heightAboveHand;
        followSpeed = source.followSpeed;
        hideWhenHandLost = source.hideWhenHandLost;
        enabled = source.enabled;
    }

    // Shows or hides the panel depending on whether its hand is tracked.
    private void UpdateVisibility()
    {
        if (!hideWhenHandLost || panelCanvas == null)
        {
            return;
        }

        bool handTracked = IsHandTracked();
        if (handTracked)
        {
            hasSeenHand = true;
        }

        // Only start hiding once the hand has been seen, so the panel still shows in the Editor without a headset.
        bool shouldShow = handTracked || !hasSeenHand;
        if (panelCanvas.enabled != shouldShow)
        {
            panelCanvas.enabled = shouldShow;
        }
    }

    // Asks the Meta SDK whether this hand is currently tracked (same check Meta's own OVRHand script uses).
    private bool IsHandTracked()
    {
        OVRPlugin.HandState handState = new OVRPlugin.HandState();
        if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, handType, ref handState))
        {
            return (handState.Status & OVRPlugin.HandStatus.HandTracked) != 0;
        }
        return false;
    }
}
