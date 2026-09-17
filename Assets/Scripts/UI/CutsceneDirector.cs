using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneDirector : MonoBehaviour
{
    [Serializable]
    public class Pane
    {
        public string label = "Shot";
        public CinemachineCamera shotCamera;
        public Animator rig;

        public string state = "Shot";
        public float leadIn = 0.75f;
        public float holdOut = 0.35f;
        [TextArea(4, 12)]
        public string text;
    }

    [SerializeField] Pane[] panes;
    [SerializeField] Typewriter typewriter;
    [SerializeField] CanvasGroup textbox;
    [SerializeField] string nextScene = "Level";

    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        textbox.alpha = 0f;
        typewriter.Clear();

        foreach (Pane pane in panes)
        {
            Cut(pane);

            if (pane.leadIn > 0f)
                yield return new WaitForSecondsRealtime(pane.leadIn);

            textbox.alpha = 1f;

            bool done = false;
            void OnFinished() => done = true;

            typewriter.Finished += OnFinished;
            typewriter.Play(pane.text);
            while (!done) yield return null;
            typewriter.Finished -= OnFinished;

            textbox.alpha = 0f;
            typewriter.Clear();

            if (pane.holdOut > 0f)
                yield return new WaitForSecondsRealtime(pane.holdOut);
        }

        SceneManager.LoadScene(nextScene);
    }

    void Cut(Pane pane)
    {
        foreach (Pane p in panes)
        {
            if (p.shotCamera != null)
                p.shotCamera.Priority = p == pane ? 10 : 0;
        }

        if (pane.rig == null) return;

        pane.rig.Play(pane.state, 0, 0f);
        pane.rig.Update(0f);
    }
}
