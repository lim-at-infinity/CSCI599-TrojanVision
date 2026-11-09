using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Templates.MR;

// Builds editable UI using the Documents menu's pose, layer, size and font.
public static class DocumentViewerFactory
{
    // Space between the Documents menu and the viewer beside it, in meters (0.02 = 2 cm).
    private const float ViewerGapMeters = 0.02f;

    public static PDFViewerPanel CreatePDF(DocumentsPanel menu)
    {
        RectTransform root = CreateRoot(menu, "PDFViewerPanel");
        var panel = root.gameObject.AddComponent<PDFViewerPanel>();
        panel.titleText = CreateLabel(root, "Title", "Document", .05f, .86f, .78f, .97f, menu);
        panel.closeButton = CreateButton(root, "Close", .80f, .87f, .97f, .97f, menu);
        var page = CreateRect(root, "PageImage", .05f, .18f, .95f, .84f);
        panel.pageImage = page.gameObject.AddComponent<Image>();
        panel.pageImage.raycastTarget = false;
        panel.pageImage.preserveAspect = true;
        panel.statusText = CreateLabel(root, "Status", "No document pages available yet.",
            .08f, .3f, .92f, .7f, menu);
        panel.previousButton = CreateButton(root, "Previous", .05f, .04f, .34f, .15f, menu);
        panel.pageCounter = CreateLabel(root, "PageCounter", "0 / 0", .35f, .04f, .65f, .15f, menu);
        panel.nextButton = CreateButton(root, "Next", .66f, .04f, .95f, .15f, menu);
        return panel;
    }

    public static DocumentVideoPanel CreateVideo(DocumentsPanel menu)
    {
        RectTransform root = CreateRoot(menu, "DocumentVideoPanel");
        var panel = root.gameObject.AddComponent<DocumentVideoPanel>();
        panel.titleText = CreateLabel(root, "Title", "Video", .05f, .86f, .78f, .97f, menu);
        panel.closeButton = CreateButton(root, "Close", .80f, .87f, .97f, .97f, menu);
        var videoArea = CreateRect(root, "VideoArea", .05f, .25f, .95f, .84f);
        var video = CreateRect(videoArea, "VideoImage", 0, 0, 1, 1);
        panel.videoImage = video.gameObject.AddComponent<RawImage>();
        panel.videoImage.raycastTarget = false;
        var fitter = video.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 16f / 9f;
        panel.statusText = CreateLabel(root, "Status", "No video available yet.", .08f, .3f, .92f, .7f, menu);
        panel.timeText = CreateLabel(root, "Time", "0:00 / 0:00", .05f, .17f, .95f, .24f, menu);
        panel.backwardButton = CreateButton(root, "-10 sec", .05f, .04f, .34f, .15f, menu);
        panel.playPauseButton = CreateButton(root, "Play", .355f, .04f, .645f, .15f, menu);
        panel.playPauseText = panel.playPauseButton.GetComponentInChildren<TMP_Text>();
        panel.forwardButton = CreateButton(root, "+10 sec", .66f, .04f, .95f, .15f, menu);
        panel.player = root.gameObject.AddComponent<VideoPlayer>();
        panel.player.playOnAwake = false;
        panel.audioSource = root.gameObject.AddComponent<AudioSource>();
        panel.audioSource.playOnAwake = false;
        panel.audioSource.spatialBlend = 0;
        return panel;
    }

    private static RectTransform CreateRoot(DocumentsPanel menu, string name)
    {
        var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        // Siblings keep working when the menu is disabled.
        root.gameObject.SetActive(false);
        SynchronizeLayout(menu, root);
        root.gameObject.layer = menu.gameObject.layer;
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var originalCanvas = menu.GetComponent<Canvas>();
        if (originalCanvas != null)
        {
            canvas.worldCamera = originalCanvas.worldCamera;
            canvas.sortingOrder = originalCanvas.sortingOrder + 1;
        }
        root.gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 4;
        root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        root.gameObject.AddComponent<GraphicRaycaster>();
        var background = CreateRect(root, "Background", 0, 0, 1, 1).gameObject.AddComponent<Image>();
        background.color = new Color(.035f, .055f, .085f, .98f);
        return root;
    }

    // Refresh on every switch, including for viewers already saved in the scene.
    // A pose copied only at creation misses later Inspector changes and TurnToFace updates.
    public static void SynchronizeLayout(DocumentsPanel menu, RectTransform root)
    {
        root.SetParent(menu.transform.parent, false);
        var original = menu.transform as RectTransform;
        if (original != null)
        {
            root.anchorMin = original.anchorMin;
            root.anchorMax = original.anchorMax;
            root.pivot = original.pivot;
            root.sizeDelta = original.sizeDelta;
        }
        else
            root.sizeDelta = new Vector2(400, 500);
        root.localPosition = menu.transform.localPosition;
        root.localRotation = menu.transform.localRotation;
        root.localScale = menu.transform.localScale;

        var sourceFacing = menu.GetComponent<TurnToFace>();
        var viewerFacing = root.GetComponent<TurnToFace>();
        if (sourceFacing != null)
        {
            if (viewerFacing == null)
                viewerFacing = root.gameObject.AddComponent<TurnToFace>();
            viewerFacing.CopySettingsFrom(sourceFacing);
        }
        else if (viewerFacing != null)
            viewerFacing.enabled = false;

        // Hand-attached menus (XR-Setup): the viewer floats above the same hand as the menu.
        var sourceFollow = menu.GetComponent<HandFollowPanel>();
        var viewerFollow = root.GetComponent<HandFollowPanel>();
        if (sourceFollow != null)
        {
            if (viewerFollow == null)
                viewerFollow = root.gameObject.AddComponent<HandFollowPanel>();
            viewerFollow.CopySettingsFrom(sourceFollow);

            // Sit to the right of the menu: one panel width, plus a small gap.
            viewerFollow.sideOffset = root.rect.width * root.lossyScale.x + ViewerGapMeters;
        }
        else if (viewerFollow != null)
            viewerFollow.enabled = false;
    }

    private static RectTransform CreateRect(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text CreateLabel(Transform parent, string name, string value,
        float x0, float y0, float x1, float y1, DocumentsPanel menu)
    {
        var label = CreateRect(parent, name, x0, y0, x1, y1).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = menu.titleText != null ? menu.titleText.font : TMP_Settings.defaultFontAsset;
        label.text = value;
        label.fontSize = 22;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.richText = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string text,
        float x0, float y0, float x1, float y1, DocumentsPanel menu)
    {
        var rect = CreateRect(parent, text + "Button", x0, y0, x1, y1);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.12f, .25f, .37f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.disabledColor = new Color(.35f, .35f, .35f, .6f);
        button.colors = colors;
        CreateLabel(rect, "Label", text, .04f, .04f, .96f, .96f, menu);
        return button;
    }
}
