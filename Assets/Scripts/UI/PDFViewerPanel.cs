using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quest document reader: each PDF page is imported as a UI sprite.
public class PDFViewerPanel : MonoBehaviour
{
    public TMP_Text titleText;
    public Image pageImage;
    public TMP_Text pageCounter;
    public TMP_Text statusText;
    public Button previousButton;
    public Button nextButton;
    public Button closeButton;

    private Sprite[] pages = System.Array.Empty<Sprite>();
    private int currentPage;
    private DocumentsPanel owner;
    private bool subscribed;

    public void Open(DocumentsPanel documents, string documentTitle, Sprite[] documentPages)
    {
        owner = documents;
        pages = documentPages ?? System.Array.Empty<Sprite>();
        currentPage = 0;
        titleText.text = documentTitle;
        Subscribe();
        gameObject.SetActive(true);
        Refresh();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;
        subscribed = true;
        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        closeButton.onClick.AddListener(Close);
    }

    public void PreviousPage()
    {
        currentPage = Mathf.Max(0, currentPage - 1);
        Refresh();
    }

    public void NextPage()
    {
        currentPage = Mathf.Min(Mathf.Max(0, pages.Length - 1), currentPage + 1);
        Refresh();
    }

    private void Refresh()
    {
        Sprite page = pages.Length == 0 ? null : pages[currentPage];
        pageImage.sprite = page;
        pageImage.preserveAspect = true;
        pageImage.enabled = page != null;
        statusText.text = pages.Length == 0 ? "No document pages available yet." :
            page == null ? "This page is unavailable." : "";
        pageCounter.text = pages.Length == 0 ? "0 / 0" : $"{currentPage + 1} / {pages.Length}";
        previousButton.interactable = currentPage > 0;
        nextButton.interactable = currentPage < pages.Length - 1;
    }

    public void Close()
    {
        if (owner != null)
            owner.CloseDocument();
        else
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (!subscribed)
            return;
        if (previousButton != null)
            previousButton.onClick.RemoveListener(PreviousPage);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }
}
