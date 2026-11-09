using UnityEngine;
using UnityEngine.UI;

// =====================================================================================
// GazeDwellCursor - press buttons by looking at them.
//
// A small circle stays in the middle of the user's view. Keep it on a button and, after a
// short wait, a ring starts filling. When the ring is full the button is clicked.
// The Quest 2 has no eye tracking, so the user aims by turning their head.
//
// HOW TO SET UP
//   Select OVRCameraRig/TrackingSpace/CenterEyeAnchor and use Add Component -> "Gaze Dwell Cursor".
//   Every button on every panel works automatically, including ones created while running.
// =====================================================================================
public class GazeDwellCursor : MonoBehaviour
{
    private const int RingTextureSize = 128;

    [Header("Timing")]

    // Seconds the cursor must stay on a button before the ring appears.
    public float waitBeforeRing = 2f;

    // Seconds the ring takes to fill. The button is clicked when it is full.
    public float ringFillSeconds = 5f;

    [Header("Cursor")]

    // How big the cursor looks, in meters when it is 1 meter away.
    public float cursorSize = 0.03f;

    // How far away the cursor floats when it is not on a button, in meters.
    public float defaultDistance = 1f;

    // Buttons further away than this are ignored, in meters.
    public float maxDistance = 5f;

    // The cursor's own Canvas object, kept in the middle of the view.
    private RectTransform cursorRoot;

    // The ring that fills up while the user keeps looking at a button.
    private Image progressRing;

    // The button the cursor is on right now (null = none).
    private Button currentButton;

    // How long the cursor has been on the current button, in seconds.
    private float dwellTime = 0f;

    // True after a click, so the user must look away before the same button can be clicked again.
    private bool hasClickedCurrentButton = false;

    void Start()
    {
        BuildCursor();
    }

    // LateUpdate runs after the head and panels have moved for this frame.
    void LateUpdate()
    {
        Button button = FindButtonInView(out float distance);

        if (button != currentButton)
        {
            currentButton = button;
            dwellTime = 0f;
            hasClickedCurrentButton = false;
        }

        UpdateDwell();
        PlaceCursor(button != null ? distance : defaultDistance);
    }

    // Counts up while the cursor stays on a button, and clicks it when the ring is full.
    private void UpdateDwell()
    {
        if (currentButton == null || hasClickedCurrentButton)
        {
            progressRing.enabled = false;
            return;
        }

        dwellTime += Time.deltaTime;

        float fill = Mathf.Clamp01((dwellTime - waitBeforeRing) / ringFillSeconds);
        progressRing.enabled = fill > 0f;
        progressRing.fillAmount = fill;

        if (fill >= 1f)
        {
            hasClickedCurrentButton = true;
            progressRing.enabled = false;
            currentButton.onClick.Invoke();
        }
    }

    // Finds the closest usable button straight ahead of the user's view.
    private Button FindButtonInView(out float closestDistance)
    {
        Ray view = new Ray(transform.position, transform.forward);
        Button closest = null;
        closestDistance = maxDistance;

        foreach (Selectable selectable in Selectable.allSelectablesArray)
        {
            Button button = selectable as Button;
            if (button == null || !button.IsInteractable())
            {
                continue;
            }

            // A panel that is hidden (its hand is lost) can't be looked at.
            Canvas canvas = button.GetComponentInParent<Canvas>();
            if (canvas == null || !canvas.enabled)
            {
                continue;
            }

            RectTransform rect = button.transform as RectTransform;
            Plane buttonPlane = new Plane(rect.forward, rect.position);
            if (!buttonPlane.Raycast(view, out float distance) || distance >= closestDistance)
            {
                continue;
            }

            Vector2 pointOnButton = rect.InverseTransformPoint(view.GetPoint(distance));
            if (rect.rect.Contains(pointOnButton))
            {
                closest = button;
                closestDistance = distance;
            }
        }

        return closest;
    }

    // Keeps the cursor in the middle of the view, at the depth of what the user is looking at.
    private void PlaceCursor(float distance)
    {
        // Sit slightly in front of the button so the two don't fight to be drawn.
        float cursorDistance = Mathf.Max(0.1f, distance - 0.01f);
        cursorRoot.localPosition = new Vector3(0f, 0f, cursorDistance);

        // Grow with distance, so the cursor always looks the same size.
        float scale = cursorDistance * cursorSize / cursorRoot.sizeDelta.x;
        cursorRoot.localScale = new Vector3(scale, scale, scale);
    }

    // Builds the cursor from code, so nothing needs to be set up in the scene.
    private void BuildCursor()
    {
        GameObject root = new GameObject("GazeCursor", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");

        cursorRoot = root.GetComponent<RectTransform>();
        cursorRoot.SetParent(transform, false);
        cursorRoot.sizeDelta = new Vector2(100f, 100f);

        // Draw on top of every panel.
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;

        Sprite ring = CreateRingSprite();

        Image outline = CreateImage("Outline", ring);
        outline.color = new Color(1f, 1f, 1f, 0.8f);

        progressRing = CreateImage("Progress", ring);
        progressRing.color = new Color(0.1f, 0.5f, 1f, 1f);
        progressRing.type = Image.Type.Filled;
        progressRing.fillMethod = Image.FillMethod.Radial360;
        progressRing.fillOrigin = (int)Image.Origin360.Top;
        progressRing.enabled = false;
    }

    private Image CreateImage(string objectName, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.layer = cursorRoot.gameObject.layer;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(cursorRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    // Draws a white ring into a texture, so the project doesn't need an image file for it.
    private Sprite CreateRingSprite()
    {
        Texture2D texture = new Texture2D(RingTextureSize, RingTextureSize, TextureFormat.RGBA32, false);
        float center = (RingTextureSize - 1) / 2f;
        float outerRadius = center;
        float innerRadius = center * 0.65f;

        for (int y = 0; y < RingTextureSize; y++)
        {
            for (int x = 0; x < RingTextureSize; x++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                // Soft edges, one pixel wide, so the ring doesn't look jagged.
                float alpha = Mathf.Clamp01(outerRadius - distanceFromCenter) * Mathf.Clamp01(distanceFromCenter - innerRadius);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, RingTextureSize, RingTextureSize), new Vector2(0.5f, 0.5f));
    }
}
