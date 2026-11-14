using UnityEngine;

namespace ReStoryMoreWork;

/// <summary>A short message at the top of the screen. Uses real time, so it also shows while the game is frozen.</summary>
internal static class Toast
{
    private const float ShowSeconds = 3f;

    private static string _message;
    private static float _hideAt;
    private static GUIStyle _style;

    internal static void Show(string message)
    {
        if (!Settings.ShowMessages.Value)
            return;

        _message = message;
        _hideAt = Time.unscaledTime + ShowSeconds;
    }

    internal static void Draw()
    {
        if (_message == null || Time.unscaledTime > _hideAt)
            return;

        _style ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = Mathf.Max(16, Screen.height / 50),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        float width = Mathf.Min(Screen.width * 0.6f, 900f);
        GUIContent content = new GUIContent(_message);
        float height = _style.CalcHeight(content, width) + 12f;

        GUI.Box(new Rect((Screen.width - width) / 2f, Screen.height * 0.06f, width, height), content, _style);
    }
}
