# Documents: PDF and video viewers

Open `Assets/Scenes/TrojanVisionDemo.unity` in Unity. Select the DocumentsPanel
instance under Main Camera, then find its Documents Panel component.

1. Click **Create Document Viewers** at the bottom of the component Inspector.
   This creates two inactive sibling objects, PDFViewerPanel and DocumentVideoPanel,
   with all controls and references assigned. Save the scene. If you skip this,
   the same panels are created automatically when a document is first opened in Play mode.
2. Keep Document Data File assigned to `Assets/Data/ExampleDocuments.json`.
   Its `pdf` entry opens the page viewer; its `video` entry opens the video viewer.
   The current JSON contains a YouTube watch-page URL: that is not a video stream
   that Unity VideoPlayer can decode. The viewer explains this and still lets you Close.
3. For the PDF, export each page as PNG, import into Unity as **Sprite (2D and UI)**,
   and drag the sprites into **PDF Pages** in page order. No PDF renderer is required.
   Until then, the viewer shows an empty state with navigation disabled.
4. For video, record/import an H.264 MP4 and assign **Document Video Clip**, or put it
   at `Assets/StreamingAssets/PaperAirplane/basic-dart.mp4` and set
   **Video Streaming Assets Path** to `PaperAirplane/basic-dart.mp4`.
   Alternatively set JSON `url` to a direct playable video-file URL.
   Priority: imported clip, StreamingAssets path, then JSON URL.
5. Enter Play mode and select either document. The menu hides. Close restores it.

The media fields currently provide one PDF page set and one video for the demo.
All PDF entries share that page set, and all video entries use the configured video
override when present. The JSON URL is used per video entry when no override is assigned.
Image entries also use the page viewer. Other document types log an unsupported-type warning.

## Layout and interaction

Both viewers refresh the menu's local position, rotation, scale and canvas size
every time they open. They also reuse its TurnToFace target, rotation offset,
turning speed, axis locks and enabled state, so camera-facing behavior continues
while the menu is hidden. Their initial font comes from the menu.
Keep DocumentsPanel to the right of the physical task; adjust its Rect Transform
to position both viewers. Do not parent them inside DocumentsPanel:
they must remain active when the menu hides. You can drag either viewer from the
Hierarchy into `Assets/Prefabs/UI` to save a reusable prefab.

Each viewer includes a world-space canvas and a TrackedDeviceGraphicRaycaster.
The scene still needs its existing EventSystem with XR UI Input Module and enabled
UI interaction on the hand/controller interactors. The legacy centered VideoPopup
is stopped and hidden when a document opens; Goal Manager references are retained.

## Verification in Unity and on Quest

- Open the PDF without pages: show the empty state; Previous/Next disabled; Close returns.
- Assign two sprites: first page has Previous disabled, last page has Next disabled;
  counters and image change correctly; reopening starts on page 1.
- Open the current YouTube entry: show the explanatory message; playback disabled; Close returns.
- Assign a real MP4: loading disables playback controls; prepared video plays with audio.
- Pause, press +10 sec and -10 sec: the video remains paused at the new time.
- Repeat while playing: playback resumes after seeking. Repeated clicks are disabled while
  the decoder is completing a seek. Seeking clamps to the beginning or last playable instant.
- Close during loading or playback: video and audio stop. Reopen and verify a fresh start.
- Test a bad direct URL: show the error state and allow Close.
- Confirm hand/controller button presses and readable text on the physical Quest 2.
- Change the menu pose or scale after creating the viewers, then open each viewer:
  it must occupy the menu's current space. Move/turn the camera while a viewer is
  open and verify that its TurnToFace behavior matches the menu's settings.

The panels do not fetch or rasterize raw PDFs, and do not resolve YouTube URLs.
