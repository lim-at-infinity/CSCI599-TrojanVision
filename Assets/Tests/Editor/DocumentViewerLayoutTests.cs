using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Templates.MR;

public class DocumentViewerLayoutTests
{
    private GameObject parent;
    private Component menu;
    private RectTransform source;
    private RectTransform viewer;
    private MethodInfo synchronize;

    [SetUp]
    public void SetUp()
    {
        // Predefined Assembly-CSharp cannot be referenced by an asmdef. Resolve the
        // public factory at runtime so these tests exercise the real scene scripts.
        var assembly = Assembly.Load("Assembly-CSharp");
        parent = new GameObject("Layout test parent");
        parent.SetActive(false);
        source = new GameObject("Documents", typeof(RectTransform)).GetComponent<RectTransform>();
        source.SetParent(parent.transform, false);
        menu = source.gameObject.AddComponent(assembly.GetType("DocumentsPanel", true));
        viewer = new GameObject("Existing viewer", typeof(RectTransform)).GetComponent<RectTransform>();
        viewer.SetParent(parent.transform, false);
        synchronize = assembly.GetType("DocumentViewerFactory", true).GetMethod("SynchronizeLayout");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(parent);
    }

    private void Synchronize() => synchronize.Invoke(null, new object[] { menu, viewer });

    [Test]
    public void ExistingViewerRefreshesPoseAndRectAfterMenuChanges()
    {
        Synchronize();
        source.anchorMin = new Vector2(.2f, .3f);
        source.anchorMax = new Vector2(.7f, .8f);
        source.pivot = new Vector2(.4f, .6f);
        source.sizeDelta = new Vector2(550, 650);
        source.localPosition = new Vector3(.85f, .12f, 1.4f);
        source.localRotation = Quaternion.Euler(12, -22, 3);
        source.localScale = Vector3.one * .0015f;

        Synchronize();

        Assert.That(viewer.parent, Is.SameAs(source.parent));
        Assert.That(viewer.anchorMin, Is.EqualTo(source.anchorMin));
        Assert.That(viewer.anchorMax, Is.EqualTo(source.anchorMax));
        Assert.That(viewer.pivot, Is.EqualTo(source.pivot));
        Assert.That(viewer.sizeDelta, Is.EqualTo(source.sizeDelta));
        Assert.That(Vector3.Distance(viewer.localPosition, source.localPosition), Is.LessThan(.00001f));
        Assert.That(Quaternion.Angle(viewer.localRotation, source.localRotation), Is.LessThan(.001f));
        Assert.That(viewer.localScale, Is.EqualTo(source.localScale));
    }

    [Test]
    public void ViewerKeepsFacingSettingsAndCanFollowWhileMenuIsHidden()
    {
        var facing = source.gameObject.AddComponent<TurnToFace>();
        var target = new GameObject("Facing target");
        target.transform.SetParent(parent.transform, false);
        target.transform.position = new Vector3(2, 3, -4);
        var settings = new SerializedObject(facing);
        settings.FindProperty("m_FaceTarget").objectReferenceValue = target.transform;
        settings.FindProperty("m_TurnToFaceSpeed").floatValue = 17;
        settings.FindProperty("m_RotationOffset").vector3Value = new Vector3(3, 180, 5);
        settings.FindProperty("m_IgnoreX").boolValue = true;
        settings.ApplyModifiedPropertiesWithoutUndo();
        Synchronize();

        var viewerFacing = viewer.GetComponent<TurnToFace>();
        Assert.That(viewerFacing, Is.Not.Null);
        var copied = new SerializedObject(viewerFacing);
        foreach (string field in new[] { "m_FaceTarget", "m_TurnToFaceSpeed", "m_RotationOffset",
            "m_IgnoreX", "m_IgnoreY", "m_IgnoreZ" })
            Assert.That(SerializedProperty.DataEquals(settings.FindProperty(field), copied.FindProperty(field)),
                Is.True, field);

        source.gameObject.SetActive(false);
        target.transform.position = new Vector3(-3, 1, -2);
        Assert.That(Quaternion.Angle(viewerFacing.GetTargetRotation(viewer.position),
            facing.GetTargetRotation(source.position)), Is.LessThan(.001f));
    }

    [Test]
    public void ViewerHonorsDisabledOrRemovedFacingBehavior()
    {
        var facing = source.gameObject.AddComponent<TurnToFace>();
        facing.enabled = false;
        Synchronize();
        Assert.That(viewer.GetComponent<TurnToFace>().enabled, Is.False);
        facing.enabled = true;
        Synchronize();
        Assert.That(viewer.GetComponent<TurnToFace>().enabled, Is.True);
        UnityEngine.Object.DestroyImmediate(facing);
        Synchronize();
        Assert.That(viewer.GetComponent<TurnToFace>().enabled, Is.False);
    }
}
