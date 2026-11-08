using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DocumentVideoPanel : MonoBehaviour
{
    public TMP_Text titleText;
    public RawImage videoImage;
    public TMP_Text statusText;
    public TMP_Text timeText;
    public TMP_Text playPauseText;
    public Button playPauseButton;
    public Button backwardButton;
    public Button forwardButton;
    public Button closeButton;
    public VideoPlayer player;
    public AudioSource audioSource;

    private DocumentsPanel owner;
    private RenderTexture texture;
    private bool seeking;
    private bool resumeAfterSeek;
    private bool subscribed;

    private void Awake()
    {
        // Also works for panels created at runtime, whose fields are assigned after Awake.
        Subscribe();
    }

    private void Subscribe()
    {
        if (subscribed || player == null)
            return;
        subscribed = true;
        player.prepareCompleted += Prepared;
        player.errorReceived += Failed;
        player.seekCompleted += SeekCompleted;
        player.loopPointReached += Finished;
        playPauseButton.onClick.AddListener(TogglePlayback);
        backwardButton.onClick.AddListener(BackwardTenSeconds);
        forwardButton.onClick.AddListener(ForwardTenSeconds);
        closeButton.onClick.AddListener(Close);
    }

    public void Open(DocumentsPanel documents, string documentTitle, VideoClip clip,
        string streamingAssetsPath, string url)
    {
        owner = documents;
        gameObject.SetActive(true);
        Subscribe();
        player.Stop();
        seeking = false;
        titleText.text = documentTitle;
        videoImage.enabled = false;
        statusText.text = "Loading video...";
        timeText.text = "0:00 / 0:00";
        playPauseText.text = "Play";
        SetControls(false);

        player.playOnAwake = false;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = 1;
        player.SetTargetAudioSource(0, audioSource);
        player.EnableAudioTrack(0, true);

        // Imported clips and local files take precedence over a JSON web URL.
        if (clip != null)
        {
            player.source = VideoSource.VideoClip;
            player.clip = clip;
        }
        else
        {
            string source = url;
            if (!string.IsNullOrWhiteSpace(streamingAssetsPath))
                source = Path.Combine(Application.streamingAssetsPath, streamingAssetsPath).Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(source))
            {
                statusText.text = "No video available yet.";
                return;
            }
            if (Uri.TryCreate(source, UriKind.Absolute, out Uri address) &&
                (address.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
                 address.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                 address.Host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase)))
            {
                statusText.text = "This video uses a YouTube link. A playable video file is needed.";
                return;
            }
            player.source = VideoSource.Url;
            player.url = source;
        }

        if (texture == null)
        {
            texture = new RenderTexture(1280, 720, 0) { name = "Document video" };
            texture.Create();
        }
        player.targetTexture = texture;
        videoImage.texture = texture;
        player.Prepare();
    }

    private void Prepared(VideoPlayer source)
    {
        if (!isActiveAndEnabled)
            return;
        statusText.text = "";
        videoImage.enabled = true;
        var fitter = videoImage.GetComponent<AspectRatioFitter>();
        if (fitter != null && source.height > 0)
            fitter.aspectRatio = (float)source.width / source.height;
        source.Play();
        SetControls(true);
    }

    public void TogglePlayback()
    {
        if (!player.isPrepared || seeking)
            return;
        if (player.isPlaying)
            player.Pause();
        else
        {
            if (player.length > 0 && player.time >= player.length - 0.1)
                player.time = 0;
            player.Play();
        }
    }

    public void BackwardTenSeconds() => Seek(-10);
    public void ForwardTenSeconds() => Seek(10);

    private void Seek(double seconds)
    {
        if (!player.isPrepared || !player.canSetTime || seeking || player.length <= 0)
            return;
        double target = Math.Max(0, Math.Min(player.length - 0.05, player.time + seconds));
        if (Math.Abs(target - player.time) < 0.01)
            return;
        resumeAfterSeek = player.isPlaying;
        seeking = true;
        player.Pause();
        SetControls(false);
        player.time = target;
    }

    private void SeekCompleted(VideoPlayer source)
    {
        if (!seeking || !isActiveAndEnabled)
            return;
        seeking = false;
        if (resumeAfterSeek)
            source.Play();
        SetControls(true);
    }

    private void Finished(VideoPlayer source)
    {
        playPauseText.text = "Play";
    }

    private void Update()
    {
        if (player == null || !player.isPrepared)
            return;
        timeText.text = $"{FormatTime(player.time)} / {FormatTime(player.length)}";
        playPauseText.text = player.isPlaying ? "Pause" : "Play";
    }

    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            seconds = 0;
        var time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
    }

    private void SetControls(bool ready)
    {
        playPauseButton.interactable = ready;
        backwardButton.interactable = forwardButton.interactable =
            ready && player.canSetTime && player.length > 0;
    }

    private void Failed(VideoPlayer source, string message)
    {
        source.Stop();
        seeking = false;
        videoImage.enabled = false;
        statusText.text = "Video could not be loaded. Close and try again.";
        SetControls(false);
        Debug.LogWarning("Document video: " + message, this);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (owner != null)
            owner.CloseDocument();
    }

    private void OnDisable()
    {
        seeking = false;
        if (player != null)
            player.Stop();
    }

    private void OnDestroy()
    {
        if (subscribed && player != null)
        {
            player.prepareCompleted -= Prepared;
            player.errorReceived -= Failed;
            player.seekCompleted -= SeekCompleted;
            player.loopPointReached -= Finished;
        }
        if (texture != null)
        {
            if (player != null)
                player.targetTexture = null;
            texture.Release();
            Destroy(texture);
        }
    }
}
