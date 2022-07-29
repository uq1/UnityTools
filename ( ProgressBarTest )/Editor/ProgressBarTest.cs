// Disable 'obsolete' warnings
#pragma warning disable 0618

using UnityEditor;
using UnityEngine;
using System.Threading;

using static ProgressBarAPI.API;

namespace ProgressBarTest
{
    [ExecuteInEditMode]
    public class ProgressBarTest : EditorWindow
    {
        [MenuItem("Window/Unique Tools/<<< Progress Bar Test >>>")]
        static void Open()
        {
            GetWindow<ProgressBarTest>("ProgressBarTest").Show();
        }

        void OnGUI()
        {
#if INWINDOW_PROGRESS
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 15), progress, progressText);
#endif //INWINDOW_PROGRESS

            if (GUILayout.Button("Test Progress Bar"))
            {
                Test();
            }
        }

        public void Test()
        {
            ProgressBarInit("Testing Progress Bar...");

            int testCount = 100;

            for (int i = 0; i < testCount; i++)
            {
                ProgressBarShow("Testing Progress Bar " + i + "/" + testCount + "...", (float)i / (float)testCount);
                Thread.Sleep(20);
            }

            ProgressBarEnd();
        }
    }
}