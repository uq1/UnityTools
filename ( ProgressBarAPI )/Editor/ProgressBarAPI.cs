//#define UNITY_PROGRESSBAR
//#define INWINDOW_PROGRESS

// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using System;
using System.Collections;

#if UNITY_PROGRESSBAR
using System.Reflection;
using System.Linq;
#endif //UNITY_PROGRESSBAR

namespace ProgressBarAPI
{
    [ExecuteInEditMode]
    public static class API
    {
#if UNITY_PROGRESSBAR
        private static MethodInfo m_Display = null;
        private static MethodInfo m_Clear = null;

        private static float progress = 0.0f;
        private static string progressText = "";

        public static void ProgressBarInit(string startText)
        {
            progress = 0.0f;
            progressText = startText;

            var type = typeof(Editor).Assembly.GetTypes().Where(t => t.Name == "AsyncProgressBar").FirstOrDefault();
            //var type = typeof(EditorWindow).Assembly.GetTypes().Where(t => t.Name == "AsyncProgressBar").FirstOrDefault();

            if (type != null)
            {
                m_Display = type.GetMethod("Display");
                m_Clear = type.GetMethod("Clear");
            }
        }
        public static void ProgressBarShow(string text, float percent)
        {
            if (m_Display == null || m_Clear == null)
            {
                ProgressBarInit(text);
            }

            progress = percent;
            progressText = text;

            if (m_Display != null)
            {
                m_Display.Invoke(null, new object[] { progressText, progress });
                //Debug.Log("prog " + progress);
                Canvas.ForceUpdateCanvases();
            }
        }
        public static void ProgressBarEnd()
        {
            progress = 0.0f;
            progressText = "";

            if (m_Display != null)
            {
                m_Display.Invoke(null, new object[] { progressText, progress });
                Canvas.ForceUpdateCanvases();
            }

            if (m_Clear != null)
            {
                m_Clear.Invoke(null, null);
            }

            m_Display = null;
            m_Clear = null;
        }
#elif INWINDOW_PROGRESS
        private static float progress = 0.0f;
        private static string progressText = "";

        public static void ProgressBarInit(string startText)
        {
            progress = 0.0f;
            progressText = startText;

#if INWINDOW_PROGRESS
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 15), progress, progressText);
#endif //INWINDOW_PROGRESS
        }

        public static void ProgressBarShow(string text, float percent)
        {
            progress = percent;
            progressText = text;

            //Debug.Log("prog " + progress);

            //Debug.Log("REPAINT!");
            //Repaint();
            Canvas.ForceUpdateCanvases();
        }

        public static void ProgressBarEnd()
        {
            progress = 0.0f;
            progressText = "";
        }
#else //
        public static void ProgressBarInit(string startText)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayProgressBar(startText, startText, 0);
        }

        public static void ProgressBarShow(string text, float percent)
        {
            EditorUtility.DisplayProgressBar(text, text, percent);
        }

        public static void ProgressBarEnd()
        {
            EditorUtility.ClearProgressBar();
        }
#endif //INWINDOW_PROGRESS
    }
}