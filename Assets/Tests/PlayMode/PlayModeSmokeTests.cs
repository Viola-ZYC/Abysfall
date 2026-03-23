using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayModeSmokeTests
{
    [UnityTest]
    public IEnumerator Smoke_FrameAdvances()
    {
        var startFrame = Time.frameCount;
        yield return null;
        Assert.Greater(Time.frameCount, startFrame);
    }

    [UnityTest]
    public IEnumerator Smoke_CanCreateAndDestroyGameObject()
    {
        var go = new GameObject("PlayModeSmoke");
        Assert.IsNotNull(go);
        Object.Destroy(go);
        yield return null;
        Assert.IsTrue(go == null);
    }
}
