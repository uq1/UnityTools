using UnityEditor;
using UnityEngine;

namespace NonConvexMeshCollider.Editor
{
    [CustomEditor(typeof(NonConvexMeshCollider)), CanEditMultipleObjects]
    public class NonConvexMeshColliderEditor : UnityEditor.Editor
    {
        private NonConvexMeshCollider p;
        private Color guiColor = new Color(0.72f, 1f, 0.6f);

        // Add these???
        //public bool shouldMerge = true;
        //public bool createColliderChildGameObject = true;
        //public int boxesPerEdge = 20;

        private void OnEnable()
        {
            p = target as NonConvexMeshCollider;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        
            GUI.backgroundColor = guiColor;

            if (GUILayout.Button(new GUIContent("Generate Colliders"))) {
                p.Calculate();
            }
        }
    }
}