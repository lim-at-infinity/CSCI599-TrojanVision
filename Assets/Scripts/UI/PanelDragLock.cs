using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================================
// PanelDragLock - lets the user drag a document viewer and leave it in place.
//
// Point at the panel and a grip bar appears under it. Grab the panel and it stops following
// the hand, staying upright and facing the user while it moves. Let go and it stays there.
// Closing and reopening the viewer puts it back beside the Documents menu.
//
// HOW TO SET UP (on PDFViewerPanel and DocumentVideoPanel)
//   1. Right-click the viewer -> Interaction SDK -> Add Ray Grab Interaction.
//   2. Add Component -> "Panel Drag Lock" on the same object.
// =====================================================================================
[RequireComponent(typeof(HandFollowPanel))]
public class PanelDragLock : MonoBehaviour
{
    // A grab that would put the panel further than this from the head is ignored, in meters.
    private const float MaxDistanceFromHead = 3f;

    private static readonly Color GripHoverColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color GripHeldColor = new Color(0.1f, 0.5f, 1f, 1f);

    // Meta's Grabbable, added by the Ray Grab wizard. Leave empty to use the one on this object.
    public Grabbable grabbable;

    // The Hand Follow Panel on this object.
    private HandFollowPanel follow;

    // The bar under the panel that shows it can be grabbed.
    private Image gripBar;

    // An empty object for Meta's Grabbable to move, so this script alone decides where the panel goes.
    private Transform grabProxy;

    // How many hands are pointing at or holding the panel right now.
    private int hoverCount = 0;
    private int holdCount = 0;

    // Which hand is moving the panel, and the gap between that hand's grab point and the panel.
    private int dragPointerId;
    private Vector3 grabOffset;

    void Awake()
    {
        follow = GetComponent<HandFollowPanel>();

        if (grabbable == null)
        {
            grabbable = GetComponent<Grabbable>();
        }

        if (grabbable == null)
        {
            Debug.LogWarning("PanelDragLock on '" + name + "': no Grabbable found. Run Add Ray Grab Interaction on this object.");
            return;
        }

        grabProxy = new GameObject(name + " Grab Proxy").transform;
        grabProxy.SetPositionAndRotation(transform.position, transform.rotation);
        grabbable.InjectOptionalTargetTransform(grabProxy);

        CreateGripBar();
    }

    void OnEnable()
    {
        hoverCount = 0;
        holdCount = 0;
        UpdateGripBar();

        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnPointerEvent;
        }
    }

    void OnDisable()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }

    void OnDestroy()
    {
        if (grabProxy != null)
        {
            Destroy(grabProxy.gameObject);
        }
    }

    // Meta's SDK calls this for every hover, grab, move and release on the panel.
    private void OnPointerEvent(PointerEvent pointerEvent)
    {
        switch (pointerEvent.Type)
        {
            case PointerEventType.Hover:
                hoverCount++;
                break;

            case PointerEventType.Unhover:
                hoverCount = Mathf.Max(0, hoverCount - 1);
                break;

            case PointerEventType.Select:
                holdCount++;
                if (holdCount == 1)
                {
                    StartDrag(pointerEvent);
                }
                break;

            case PointerEventType.Move:
                if (holdCount > 0 && pointerEvent.Identifier == dragPointerId)
                {
                    MovePanel(pointerEvent.Pose.position + grabOffset);
                }
                break;

            case PointerEventType.Unselect:
                ReleaseHold();
                break;

            case PointerEventType.Cancel:
                hoverCount = Mathf.Max(0, hoverCount - 1);
                ReleaseHold();
                break;
        }

        UpdateGripBar();
    }

    private void StartDrag(PointerEvent pointerEvent)
    {
        dragPointerId = pointerEvent.Identifier;
        grabOffset = transform.position - pointerEvent.Pose.position;
        follow.BeginDrag();
        Debug.Log("PanelDragLock: grabbed '" + name + "' at " + transform.position + ", grab point " + pointerEvent.Pose.position);
    }

    private void MovePanel(Vector3 target)
    {
        if (follow.head != null && Vector3.Distance(target, follow.head.position) > MaxDistanceFromHead)
        {
            return;
        }

        transform.position = target;
        follow.FaceHead();
    }

    private void ReleaseHold()
    {
        if (holdCount == 0)
        {
            return;
        }

        holdCount--;
        if (holdCount == 0)
        {
            follow.FaceHead();
            Debug.Log("PanelDragLock: released '" + name + "' at " + transform.position);
        }
    }

    // Builds the small bar that sits just under the panel's bottom edge.
    private void CreateGripBar()
    {
        RectTransform bar = new GameObject("GripBar", typeof(RectTransform)).GetComponent<RectTransform>();
        bar.SetParent(transform, false);
        bar.gameObject.layer = gameObject.layer;
        bar.anchorMin = new Vector2(0.3f, 0f);
        bar.anchorMax = new Vector2(0.7f, 0f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -8f);
        bar.sizeDelta = new Vector2(0f, 14f);

        gripBar = bar.gameObject.AddComponent<Image>();
        gripBar.raycastTarget = false;
    }

    // Shows the bar while the panel is pointed at, and turns it blue while held.
    private void UpdateGripBar()
    {
        if (gripBar == null)
        {
            return;
        }

        gripBar.enabled = hoverCount > 0 || holdCount > 0;
        gripBar.color = holdCount > 0 ? GripHeldColor : GripHoverColor;
    }
}
