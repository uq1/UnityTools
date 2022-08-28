using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static ProgressBarAPI.API;
using System;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace DrawCallOptimizer
{
    public class DrawCallOptimizer : EditorWindow
    {
        bool settingCollidersTransparent = false;
        bool settingCollidersAllowCutout = false;
        bool settingCollidersAllowGlass = true;
        bool settingCollidersHeightRangeEnabled = true;
        float settingCollidersHeightRangeMin = -9999.0f;
        float settingCollidersHeightRangeMax = 3.0f;
        bool settingConvexCollidersAllowed = true;
        
        [MenuItem("Window/Unique Tools/Draw Call Optimizer")]
        static void Open()
        {
            GetWindow<DrawCallOptimizer>("DrawCallOptimizer").Show();
        }

        void OnGUI()
        {
            minSize.Set(100, 160);

            EditorStyles.textField.wordWrap = true;
            EditorStyles.label.wordWrap = true;

            // Usage text...
            EditorGUILayout.HelpBox("Optimize the number of draw calls for the selected hierarchy.", UnityEditor.MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("By merging all the meshes into a single mesh per material, we can optimize the render speed of the asset.\n\nOnce optimized, we then generate optimized colliders for the new meshes, ignoring all unreachable objects by default.");

            EditorGUILayout.Space();

            if (GUILayout.Button("Optimize Selected Tree"))
            {
                OptimizeTree();
            }

            EditorGUILayout.HelpBox("Use the Regenerate Colliders button to delete and regenerate all colliders after changing options.", UnityEditor.MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("By default transparent materials will be skipped. This can be overridden with these options.");

            EditorGUILayout.Space();

            settingCollidersTransparent = EditorGUILayout.Toggle("Transparent Colliders?", settingCollidersTransparent);
            
            settingCollidersAllowCutout = EditorGUILayout.Toggle("CutOut Colliders?", settingCollidersAllowCutout);
            
            settingCollidersAllowGlass = EditorGUILayout.Toggle("\"Glass\" colliders?", settingCollidersAllowGlass);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("By default we will only generate colliders at reachable levels. This can be overridden with these options.");

            EditorGUILayout.Space();

            settingCollidersHeightRangeEnabled = EditorGUILayout.Toggle("Use Height Range?", settingCollidersHeightRangeEnabled);
            
            settingCollidersHeightRangeMin = EditorGUILayout.FloatField("Min Height", settingCollidersHeightRangeMin);
            settingCollidersHeightRangeMax = EditorGUILayout.FloatField("Max Height", settingCollidersHeightRangeMax);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Convex colliders reduce the CPU usage on collisions at the cost of accuracy. Will only be used on small objects < 20cm if enabled.");

            EditorGUILayout.Space();

            settingConvexCollidersAllowed = EditorGUILayout.Toggle("Allow Convex Colliders?", settingConvexCollidersAllowed);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Colliders"))
            {
                OptimizeCollidersCallback();
            }

            if (GUILayout.Button("Calculate Missing Normals"))
            {
                CalculateNormalsCallback();
            }
        }
        
        void OptimizeTree()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("DrawCallOptimizer:: No selected object.");
                return;
            }

            var sourceObject = Selection.activeGameObject;
            
            GameObject GO = sourceObject.gameObject;
            
            var filters = GO.GetComponentsInChildren<MeshFilter>();
            int numOriginalDrawCalls = 0;
            
            foreach (MeshFilter filter in filters)
            {
                var r = filter.gameObject.GetComponent<MeshRenderer>();
                var materials = r.sharedMaterials;
                numOriginalDrawCalls += materials.Length;
            }
            
            // First clean out any empty game objects...
            CleanupEmptyGameObjects(GO);
            
            // Check for any combined material meshes, and split them as required, so they can be optimized...
            //Debug.Log("FixMissingMaterials");
            FixMissingMaterials(GO);
            
            if (!CheckMeshAccessability(GO))
            {
                Debug.Log("DrawCallOptimizer: Reading of some meshes was denied by the creator. Please slap them for their lack of openness. This may not work.");
            }
            
            // First, destroy all old colliders...
            //Debug.Log("DestroyColliders");
            DestroyColliders(GO);
        
            //Debug.Log("SplitMeshesIfRequired");
            SplitMeshesIfRequired(GO);
            
            //Debug.Log("CleanupDisabledRenderers");
            CleanupDisabledRenderers(GO);
            
            // Combine all meshes with the same materials to minimize draw calls...
            //Debug.Log("CombineMeshes");
            CombineMeshes(GO);
            
            //Debug.Log("CleanupDisabledRenderers");
            CleanupDisabledRenderers(GO);
            
            // Clean out any empty game objects, after our work is done...
            CleanupEmptyGameObjects(GO);
            
            //Debug.Log("CheckRecalculateNormals");
            CheckRecalculateNormals(GO);
            
            var filters2 = GO.GetComponentsInChildren<MeshFilter>();
            int numOptimizedDrawCalls = 0;
            
            foreach (MeshFilter filter in filters2)
            {
                var r = filter.gameObject.GetComponent<MeshRenderer>();
                var materials = r.sharedMaterials;
                numOptimizedDrawCalls += materials.Length;
            }
            
            Debug.Log("DrawCallOptimizer: Optimized " + numOriginalDrawCalls + " original CUA draw calls into " + numOptimizedDrawCalls + " optimized draw calls.");
            
            // Generate new colliders...
            //Debug.Log("OptimizeColliders");
            OptimizeColliders(GO);
            
            Debug.Log("DrawCallOptimizer: New colliders generated OK...");

            AssetDatabase.SaveAssets();
        }
        
        void CalculateNormalsCallback()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("DrawCallOptimizer:: No selected object.");
                return;
            }

            var sourceObject = Selection.activeGameObject;
            
            GameObject GO = sourceObject.gameObject;
            
            if (!CheckMeshAccessability(GO))
            {
                Debug.Log("DrawCallOptimizer: Reading of meshes was denied by the creator. Please slap them for their lack of community openness. This may not work.");
            }
            
            CleanupDisabledRenderers(GO);
            
            CheckRecalculateNormals(GO);

            AssetDatabase.SaveAssets();
        }

        void OptimizeCollidersCallback()
        {
            if (Selection.activeGameObject == null)
            {
                Debug.LogError("DrawCallOptimizer:: No selected object.");
                return;
            }

            var sourceObject = Selection.activeGameObject;
            
            GameObject GO = sourceObject.gameObject;
            
            if (!CheckMeshAccessability(GO))
            {
                Debug.Log("DrawCallOptimizer: Reading of meshes was denied by the creator. Please slap them for their lack of community openness. This may not work.");
            }
            
            // First, destroy all old colliders...
            DestroyColliders(GO);
            
            CleanupDisabledRenderers(GO);

            // Generate new colliders...
            OptimizeColliders(GO);
            
            Debug.Log("DrawCallOptimizer: New colliders generated OK...");

            AssetDatabase.SaveAssets();
        }

        void OptimizeColliders(GameObject sourceObject)
        {
            // Some sanity checking...
            if (settingCollidersHeightRangeMin > settingCollidersHeightRangeMax)
            {
                settingCollidersHeightRangeMin = settingCollidersHeightRangeMax - 1.0f;
            }
            
            if (settingCollidersHeightRangeMax < settingCollidersHeightRangeMin)
            {
                settingCollidersHeightRangeMax = settingCollidersHeightRangeMin + 1.0f;
            }
            
            //
            // Do all mesh filter/renderer colliders...
            //
            var filters = sourceObject.GetComponentsInChildren<MeshFilter>();
            
            //Debug.Log("Found " + filters.Length + " MeshFilters.");
            
            if (filters != null)
            {
                foreach (MeshFilter filter in filters)
                {
                    if (!filter.sharedMesh || !filter.sharedMesh.isReadable)
                    {
                        continue;
                    }
                    
                    GameObject obj = filter.gameObject;
                    //Debug.Log("Found " + obj.name);
                    
                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                    
                    if (filter.sharedMesh == null || renderer == null)
                    {
                        //return;
                        continue;
                    }
                    
                    GenerateCollider(obj, filter.sharedMesh, renderer.sharedMaterials);
                }
            }
            
            //
            // Do all skinned mesh colliders...
            //
            /*var skinnedRenderers = sourceObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            //Debug.Log("Found " + skinnedRenderers.Length + " SkinnedMeshRenderers.");
            
            if (skinnedRenderers != null)
            {
                foreach (SkinnedMeshRenderer smr in skinnedRenderers)
                {
                    GameObject obj = smr.gameObject;
                    //Debug.Log("Found " + obj.name);

                    if (smr.sharedMesh == null || smr.sharedMaterials == null)
                    {
                        return;
                    }
                    
                    GenerateCollider(obj, smr.sharedMesh, smr.sharedMaterials);
                }
            }*/
        }
        
        bool MaterialValidForCollider(Material material)
        {
            if (material == null)
            {
                return false;
            }
            
            if (!settingCollidersTransparent)
            {// Transparent material collisions are disabled...
                var blendMode = (int)Math.Round(material.GetFloat("_Mode")); // WTF is this a FLOAT, Unity!?!?!?!?!!?
                //var blendMode = material.GetInt("_Mode");
                
                if (settingCollidersAllowCutout && blendMode == 1 /*Cutout*/)
                {// Allow cutout...
                    
                }
                else if (settingCollidersAllowGlass && material.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0)
                {// Allow glass...
                
                }
                /*else if (material.renderQueue == (int)UnityEngine.Rendering.RenderQueue.Transparent)
                {// Skip transparent objects...
                    return false;
                }*/
                else if (blendMode >= 2 /* Fade/Trans*/ || blendMode == 1 /*Cutout*/)
                {// Skip transparent objects...
                    return false;
                }
                
            }

            return true;
        }
        
        bool GenerateCollider(GameObject GO, Mesh sharedMesh, Material[] sharedMaterials)
        {
            try
            {
                if (sharedMesh == null)
                {
                    return false;
                }
                
                var totalsubmeshcount = sharedMaterials.Length;
                
                if (totalsubmeshcount <= 0)
                {// Nothing to do here...
                    return false;
                }
                
                // move required submeshes lower
                var materials = sharedMaterials;
                
                if (materials == null || materials.Length < 1)
                {
                    Debug.Log(sharedMesh.name + " has no materials.");
                    return false;
                }
                
                // Check if there are any valid surfaces here...
                bool foundValidMateiral = false;
                
                for (int i = 0; i < totalsubmeshcount; i++)
                {
                    if (MaterialValidForCollider(materials[i]))
                    {
                        foundValidMateiral = true;
                        break;
                    }
                }
                
                if (!foundValidMateiral)
                {// Nothing to do here...
                    Debug.Log(sharedMesh.name + " has no valid collider materials, based on your settings.");
                    return false;
                }
                
                Quaternion OldRot = GO.transform.rotation;
                Vector3 OldPos = GO.transform.position;

                GO.transform.rotation = Quaternion.identity;
                GO.transform.position = Vector3.zero;

                var originalMeshName = sharedMesh.name;

                // Make the new mesh...
                var newMesh = new Mesh();
                //newMesh.name = string.Format("GeneratedCollider_{0}", originalMeshName);
                newMesh.name = "GeneratedCollider_" + materials[0].name;//originalMeshName;
                newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                if (!AssetDatabase.IsValidFolder("Assets/Optimized Collider Meshes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Optimized Collider Meshes");
                }

                var prefabPath = "";

                var new_mesh_name = string.Format("ColliderMesh_{0}_{1}_{2}",
                    GO.name, originalMeshName, (int)Mathf.Abs(newMesh.GetHashCode()));

                newMesh.name = new_mesh_name;
                prefabPath = "Assets/Optimized Collider Meshes/" + new_mesh_name + ".asset";

                List<int> newTris = new List<int>();
                List<Vector3> newVerts = new List<Vector3>();
                List<Vector3> newNormals = new List<Vector3>();
                
                int current_vert = 0;

                for (int i = 0; i < totalsubmeshcount; i++)
                {
                    if (!MaterialValidForCollider(materials[i]))
                    {// Skip this one...
                        continue;
                    }
                    
                    var oldVerts = new List<Vector3>();
                    sharedMesh.GetVertices(oldVerts);

                    var oldTris = sharedMesh.GetTriangles(i);

                    var oldNormals = new List<Vector3>();
                    sharedMesh.GetNormals(oldNormals);
                    
                    bool haveNormals = true;
                    
                    if (oldNormals.Count <= 0)
                    {
                        haveNormals = false;
                    }
                    
                    var vertReplacements = new List<int>();
                    for (int j = 0; j < oldVerts.Count; j++)
                    {
                        vertReplacements.Add(-1);
                    }

                    for (int j = 0; j < oldTris.Length; j += 3)
                    {
                        Vector3 oldNormal1 = new Vector3(0,0,0);
                        Vector3 oldNormal2 = new Vector3(0,0,0);
                        Vector3 oldNormal3 = new Vector3(0,0,0);
                        
                        var oldTri1 = oldTris[j];
                        var oldVert1 = oldVerts[oldTri1];
                        
                        if (haveNormals) oldNormal1 = oldNormals[oldTri1];

                        var oldTri2 = oldTris[j + 1];
                        var oldVert2 = oldVerts[oldTri2];
                        if (haveNormals) oldNormal2 = oldNormals[oldTri2];

                        var oldTri3 = oldTris[j + 2];
                        var oldVert3 = oldVerts[oldTri3];
                        if (haveNormals) oldNormal3 = oldNormals[oldTri3];
                        
                        int found1 = vertReplacements[oldTri1];
                        int found2 = vertReplacements[oldTri2];
                        int found3 = vertReplacements[oldTri3];

                        if (settingCollidersHeightRangeEnabled && oldVert1.y < settingCollidersHeightRangeMin && oldVert2.y < settingCollidersHeightRangeMin && oldVert3.y < settingCollidersHeightRangeMin)
                        {
                            continue;
                        }
                        
                        if (settingCollidersHeightRangeEnabled && oldVert1.y > settingCollidersHeightRangeMax && oldVert2.y > settingCollidersHeightRangeMax && oldVert3.y > settingCollidersHeightRangeMax)
                        {
                            continue;
                        }

                        if (found1 >= 0)
                        {// Reuse the old vert...
                            newTris.Add(found1);
                        }
                        else
                        {
                            newVerts.Add(oldVert1);
                            if (haveNormals) newNormals.Add(oldNormal1);
                            newTris.Add(current_vert);
                            current_vert++;
                            
                            vertReplacements[oldTri1] = newVerts.Count - 1;
                        }

                        if (found2 >= 0)
                        {// Reuse the old vert...
                            newTris.Add(found2);
                        }
                        else
                        {
                            newVerts.Add(oldVert2);
                            if (haveNormals) newNormals.Add(oldNormal2);
                            newTris.Add(current_vert);
                            current_vert++;
                            
                            vertReplacements[oldTri2] = newVerts.Count - 1;
                        }

                        if (found3 >= 0)
                        {// Reuse the old vert...
                            newTris.Add(found3);
                        }
                        else
                        {
                            newVerts.Add(oldVert3);
                            if (haveNormals) newNormals.Add(oldNormal3);
                            newTris.Add(current_vert);
                            current_vert++;
                            
                            vertReplacements[oldTri3] = newVerts.Count - 1;
                        }
                    }
                }
                
                if (current_vert <= 0 || newVerts.Count <= 0 || newTris.Count <= 0)
                {// Seems there was nothing to add (max height most likely)...
                    newMesh = null;
                    
                    GO.transform.rotation = OldRot;
                    GO.transform.position = OldPos;
                    return false;
                }
                else
                {
                    newMesh.SetVertices(newVerts);
                    newMesh.SetTriangles(newTris, 0);
                    newMesh.SetNormals(newNormals);
                    
                    var collider = GO.AddComponent<MeshCollider>();
                    collider.sharedMesh = null;
                    collider.sharedMesh = newMesh;
                    
                    newMesh.RecalculateBounds();
                    
                    bool useConvex = false;
                    
                    if (newMesh.bounds.size.x <= 0.1f && newMesh.bounds.size.y <= 0.1f && newMesh.bounds.size.z <= 0.1f)
                    {
                        //Debug.Log("Mesh " + newMesh.name + " is small (" + newMesh.bounds.size.x + " x " + newMesh.bounds.size.y + " x " + newMesh.bounds.size.z + "), will use a convex collider.");
                        useConvex = true;
                    }
                    else
                    {
                        //Debug.Log("Mesh " + newMesh.name + " is large (" + newMesh.bounds.size.x + " x " + newMesh.bounds.size.y + " x " + newMesh.bounds.size.z + "), will not use a convex collider.");
                    }
                    
                    if (settingConvexCollidersAllowed && useConvex)
                    {// See if we can make a convex collider...
                        try
                        {// Inside the try in case it fails, it should fall back...
                            collider.inflateMesh = true;
                            collider.skinWidth = 0.0001f;
                            collider.convex = true;
                            
                            //Debug.Log("GenerateCollider: Created collider mesh " + newMesh.name + " with " + newVerts.Count + " verts and " + newTris.Count + " tris.");
                            //Debug.Log("GenerateCollider: A convex collider was generated successfully from " + newMesh.name + ".");
                        }
                        catch (Exception e)
                        {
                            collider.inflateMesh = false;
                            collider.convex = false;
                            
                            //Debug.Log("GenerateCollider: Created collider mesh " + newMesh.name + " with " + newVerts.Count + " verts and " + newTris.Count + " tris.");
                            //Debug.Log("GenerateCollider: Convex collider failed to generate for this mesh, using full mesh for collisions.");
                        }
                    }
                    else
                    {
                        //Debug.Log("GenerateCollider: Created collider mesh " + newMesh.name + " with " + newVerts.Count + " verts and " + newTris.Count + " tris.");
                    }
                    
                    GO.transform.rotation = OldRot;
                    GO.transform.position = OldPos;

                    AssetDatabase.CreateAsset(newMesh, prefabPath);
                    AssetDatabase.SaveAssets();

                    /*simplifiedMesh.name = newMesh.name;
                    collider.sharedMesh = simplifiedMesh;
                    AssetDatabase.CreateAsset(simplifiedMesh, prefabPath);
                    AssetDatabase.SaveAssets();*/
                }
            }
            catch (Exception e)
            {
                // Get stack trace for the exception with source file information
                //var st = new StackTrace(e, true);
                // Get the top stack frame
                //var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                //var line = frame.GetFileLineNumber();
                
                //Debug.LogError("Exception caught: line: " + line + "\n" + e);
                Debug.LogError("Exception caught: " + e);
            }
            
            return true;
        }
        
        void DestroyColliders(GameObject sourceObject)
        {
            if (sourceObject != null)
            {
                var oldBoxColls = sourceObject.GetComponentsInChildren<BoxCollider>();
                
                foreach (BoxCollider collider in oldBoxColls)
                {
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }
                }
                
                var oldCapsuleColls = sourceObject.GetComponentsInChildren<CapsuleCollider>();
                
                foreach (CapsuleCollider collider in oldCapsuleColls)
                {
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }
                }
                
                var oldSphereColls = sourceObject.GetComponentsInChildren<SphereCollider>();
                
                foreach (SphereCollider collider in oldSphereColls)
                {
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }
                }
                
                var oldTerrainColls = sourceObject.GetComponentsInChildren<TerrainCollider>();
                
                foreach (TerrainCollider collider in oldTerrainColls)
                {
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }
                }
                
                var colliders = sourceObject.GetComponentsInChildren<MeshCollider>();
                
                if (colliders != null && colliders.Length > 0)
                {
                    foreach (MeshCollider collider in colliders)
                    {
                        if (collider != null)
                        {
                            //Debug.Log("deleting collider: " + collider.sharedMesh.name);
                            DestroyImmediate(collider);
                        }
                    }
                }
            }
        }
        
        //
        // Mesh Combiner/Optimizer...
        //
        
        class CombineData
        {
            public Material material;
            public List<MeshFilter> filter = new List<MeshFilter>();
            public List<MeshRenderer> renderer = new List<MeshRenderer>();
            public int lightmapIndex;
        }
        
        public bool is32bit = true;
        
        void CombineMeshes(GameObject combineParent)
        {
            try
            {
                // Verify there is existing object root, otherwise bail.
                if (combineParent == null)
                {
                    Debug.Log("DrawCallOptimizer: Parent of objects to combne not assigned. Operation cancelled.");
                    return;
                }

                // Remember the original position of the object. 
                // For the operation to work, the position must be temporarily set to (0,0,0).
                Vector3 originalPosition = combineParent.transform.position;
                combineParent.transform.position = Vector3.zero;

                // Locals
                List<CombineData> combinableMeshesList = new List<CombineData>();
                List<GameObject> combinedObjects = new List<GameObject>();
                
                //Debug.Log("DEBUG: Checking validity of meshes for combining...");

                MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();
                
                // Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
                foreach (var meshFilter in meshFilters)
                {
                    var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                    
                    if (!meshFilter.sharedMesh || !meshFilter.sharedMesh.isReadable)
                    {
                        continue;
                    }
                    
                    if (meshRenderer == null)
                    {
                        Debug.Log("DrawCallOptimizer: The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
                        continue;
                    }
                    
                    if (!meshRenderer.enabled)
                    {
                        continue;
                    }
                    
                    // First, clean up materials and change any material instances back to the base material...
                    if (meshRenderer.sharedMaterials.Length < 1 || meshRenderer.sharedMaterials[0] == null)
                    {
                        //combineParent.transform.position = originalPosition;
                        Debug.Log("DrawCallOptimizer: Material on mesh renderer " + meshRenderer.gameObject.name + " is null, skipping optimize on that renderer.");
                        continue;
                    }
                    
                    var materials = meshRenderer.sharedMaterials;
                    
                    if (materials == null)
                    {
                        //combineParent.transform.position = originalPosition;
                        Debug.Log("DrawCallOptimizer: The Mesh Renderer on object " + meshFilter.name + " has no material assigned. Skipping.");
                        continue;
                    }

                    // Continue...
                    var material = materials[0];
                    
                    //Debug.Log("MAT: " + material.name);

                    bool added = false;
                    
                    // Add material to mesh filter mapping to dictionary
                    foreach (var entry in combinableMeshesList)
                    {
                        if (entry.material == material)
                        {
                            if (entry.lightmapIndex == meshRenderer.lightmapIndex)
                            {// Only combine if the lightmap index matches, otherwise make a new copy of the material so lightmaps work right...
                                //Debug.Log("entry.lightmapIndex == meshRenderer.lightmapIndex");
                                entry.filter.Add(meshFilter);
                                entry.renderer.Add(meshRenderer);
                                added = true;
                                //Debug.Log("append " + material.name + " lm " + meshRenderer.lightmapIndex);
                                break;
                            }
                            /*else
                            {
                                Debug.Log("entry.lightmapIndex != meshRenderer.lightmapIndex");
                            }*/
                        }
                    }
                    
                    if (!added)
                    {
                        //Debug.Log("new entry " + material.name + " lm " + meshRenderer.lightmapIndex);
                        
                        CombineData newData = new CombineData();
                        newData.material = material;
                        newData.filter.Add(meshFilter);
                        newData.renderer.Add(meshRenderer);
                        newData.lightmapIndex = meshRenderer.lightmapIndex;
                        combinableMeshesList.Add(newData);
                    }
                }
                
                //Debug.Log("DEBUG: Merging materials...");

                // For each material, create a new merged object, in the scene and in the assets folder.
                foreach (var entry in combinableMeshesList)
                {
                    List<MeshFilter> filterList = entry.filter;
                    List<MeshRenderer> rendererList = entry.renderer;
                    
                    //Debug.Log("DEBUG: dah");
                    
                    if (entry.material == null)
                    {
                        Debug.Log("DEBUG: shit");
                    }
                    
                    // Create a convenient material name
                    string materialName = entry.material.ToString().Split(' ')[0];
                    
                    //Debug.Log("DEBUG: materialName" + materialName);
                    
                    CombineInstance[] combine = new CombineInstance[filterList.Count];
                    
                    for (int i = 0; i < filterList.Count; i++)
                    {
                        //Debug.Log("DEBUG: begin filterList " + (int)(i+1) + " / " + filterList.Count);
                        
                        combine[i].mesh = filterList[i].sharedMesh;
                        combine[i].transform = filterList[i].transform.localToWorldMatrix;
                        combine[i].lightmapScaleOffset = rendererList[i].lightmapScaleOffset;
                        combine[i].realtimeLightmapScaleOffset = rendererList[i].realtimeLightmapScaleOffset;
                    }
                    
                    //Debug.Log("DEBUG: combine set up for " + materialName);

                    // Create a new mesh using the combined properties
                    var format = is32bit? IndexFormat.UInt32 : IndexFormat.UInt16;
                    
                    Mesh combinedMesh = new Mesh { indexFormat = format };
                    combinedMesh.CombineMeshes(combine, true, true, true); // UQ: Do lightmap too!!!
                    
                    //Debug.Log("DEBUG: combined " + materialName);

                    // Create asset
                    materialName += "_" + combinedMesh.GetInstanceID();

                    //Debug.Log("DEBUG: new material name " + materialName);

                    // Create asset
                    materialName += "_" + combinedMesh.GetInstanceID();

                    if (!AssetDatabase.IsValidFolder("Assets/Optimized Meshes"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Optimized Meshes");
                    }

                    AssetDatabase.CreateAsset(combinedMesh, "Assets/Optimized Meshes/CombinedMeshes_" + materialName + ".asset");

                    // Create game object
                    string goName = (combinableMeshesList.Count > 1)? "CombinedMeshes_" + materialName : "CombinedMeshes_" + combineParent.name;
                    GameObject combinedObject = new GameObject(goName);
                    var filter = combinedObject.AddComponent<MeshFilter>();
                    filter.sharedMesh = combinedMesh;
                    var renderer = combinedObject.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = entry.material;
                    combinedObjects.Add(combinedObject);
                    renderer.lightmapIndex = entry.lightmapIndex;
                    
                    //Debug.Log("DEBUG: finished combine for " + materialName);
                }
                
                //Debug.Log("DEBUG: Clearing old gameobject...");

                GameObject resultGO = new GameObject("CombinedMeshes_" + combineParent.name);
                
                // Rebuild the gameobject...
                foreach (var combinedObject in combinedObjects)
                {
                    combinedObject.transform.parent = resultGO.transform;
                }
                
                // Return to original positions
                combineParent.transform.position = originalPosition;
                resultGO.transform.position = originalPosition;
                //resultGO.transform.position = Vector3.zero;
                resultGO.transform.parent = combineParent.transform;
                
                foreach (var entry in combinableMeshesList)
                {
                    List<MeshRenderer> rendererList = entry.renderer;
                    
                    foreach (MeshRenderer r in rendererList)
                    {
                        r.enabled = false;
                    }
                }

                // Create prefab
                if (!AssetDatabase.IsValidFolder("Assets/Optimized Meshes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Optimized Meshes");
                }

                Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/Optimized Meshes/" + resultGO.name + ".prefab");
                PrefabUtility.ReplacePrefab(resultGO, prefab, ReplacePrefabOptions.ConnectToPrefab);
            }
            catch (System.ArgumentException e) 
            {
                Debug.LogError("ArgumentException: " + e);
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError("IndexOutOfRangeException: " + e);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception caught: " + e);
            }
        }
        
        //
        // Mesh splitting...
        //
        
        void SplitMeshesIfRequired(GameObject combineParent)
        {
            // Verify there is existing object root, otherwise bail.
            if (combineParent == null)
            {
                Debug.Log("DrawCallOptimizer: Parent of objects to combne not assigned. Operation cancelled.");
                return;
            }
            
            List<Transform> transforms = new List<Transform>();
            MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();
            
            // Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
            foreach (var meshFilter in meshFilters)
            {
                if (!meshFilter.sharedMesh || !meshFilter.sharedMesh.isReadable)
                {
                    continue;
                }
                
                var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                
                if (meshRenderer == null)
                {
                    //Debug.Log("DrawCallOptimizer: The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
                    continue;
                }
                
                if (!meshRenderer.enabled)
                {
                    continue;
                }
                
                var materials = meshRenderer.sharedMaterials;
                
                if (materials == null)
                {
                    Debug.Log("DrawCallOptimizer: The Mesh Renderer on object " + meshFilter.name + " has no material assigned. Skipping.");
                    continue;
                }
                
                if (meshFilter.transform == null || meshFilter.transform.Equals(null))
                {
                    Debug.Log("DrawCallOptimizer: " + meshFilter.name + "'s game object has a null transform.");
                }
                
                if (materials.Length > 1)
                {
                    transforms.Add(meshFilter.transform);
                }
            }
            
            ApplyTransform(transforms.ToArray());
        }
        
        // Apply individual transforms for multiple selected transforms.
        // It will apply the top-level transforms first, then update
        // children, but not siblings if those aren't selected.
        public static void ApplyTransform(Transform[] transforms)
        {
            bool[] applied = new bool[transforms.Length];
            for (int i = 0; i < applied.Length; ++i) applied[i] = false;

            int applyCount = 0;

            while (applyCount != transforms.Length)
            {
                // Pass over list, finding unapplied transforms with no parents or applied parents.
                for (int i = 0; i < transforms.Length; ++i)
                {
                    // If this entry is unapplied...
                    if (!applied[i])
                    {
                        bool canApply = true; // assume we can apply the transform.

                        // Is the entry a child of an unapplied parent?
                        for (int j = 0; j < transforms.Length; ++j)
                        {
                            if (i == j) continue; // ignore same entry.

                            // If it's a child of unapplied parent,
                            // we can't apply the transform to [i] in this pass.
                            if (transforms[i].IsChildOf(transforms[j]) && !applied[j])
                            {
                                canApply = false;
                                break;
                            }
                        }

                        if (canApply)
                        {
                            ApplyTransform(transforms[i]);
                            applied[i] = true;
                            //Debug.Log("DrawCallOptimizer: Applied transform to " + transforms[i].name);
                            ++applyCount;
                        }
                    }
                }
            }
        }

        // Apply an individual transform.
        public static void ApplyTransform(Transform transform)
        {
            if (transform == null)
            {
                return;
            }
            
            try
            {
                var meshFilter = transform.GetComponent<MeshFilter>();
                var meshRenderer = transform.GetComponent<MeshRenderer>();
                
                if (meshFilter == null || meshRenderer == null)
                {
                    return;
                }
                
                if (!meshRenderer.enabled)
                {
                    return;
                }
                
                if (meshFilter.sharedMesh == null)
                {
                    return;
                }
                
                if (!meshFilter.sharedMesh.isReadable)
                {
                    return;
                }
                
                //Debug.Log("MeshSplit:: Splitting mesh for object (" + transform.name + ").");
                string originalMeshName = meshFilter.sharedMesh.name;
                
                // move required submeshes lower
                var totalsubmeshcount = meshRenderer.sharedMaterials.Length;

                List<Vector2> uv1s = new List<Vector2>();
                List<Vector2> uv2s = new List<Vector2>();
                List<Vector2> uv3s = new List<Vector2>();
                List<Vector2> uv4s = new List<Vector2>();
                meshFilter.sharedMesh.GetUVs(0, uv1s);
                meshFilter.sharedMesh.GetUVs(1, uv2s);
                meshFilter.sharedMesh.GetUVs(2, uv3s);
                meshFilter.sharedMesh.GetUVs(3, uv4s);

                var materials = meshRenderer.sharedMaterials;

                for (int i = 0; i < totalsubmeshcount; i++)
                {
                    var newMesh = new Mesh();//Instantiate(meshFilter.sharedMesh);
                    newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    
                    //Debug.Log("MeshSplit:: Splitting mesh for object (" + transform.name + ") - subMesh " + (int)(i+1) + "/" + totalsubmeshcount);

                    string materialName = materials[i].name;
                    string new_mesh_name = "SplitMesh_" + materialName + "_" + (int)Mathf.Abs(newMesh.GetHashCode());
                    newMesh.name = new_mesh_name;
                    
                    //Debug.Log("DEBUG: hash");

                    var oldVerts = new List<Vector3>();
                    meshFilter.sharedMesh.GetVertices(oldVerts);
                    
                    var vertReplacements = new List<int>();
                    for (int j = 0; j < oldVerts.Count; j++)
                    {
                        vertReplacements.Add(-1);
                    }
                    
                    //Debug.Log("DEBUG: verts");

                    var oldTris = meshFilter.sharedMesh.GetTriangles(i);

                    var oldNormals = new List<Vector3>();
                    meshFilter.sharedMesh.GetNormals(oldNormals);
                    
                    bool haveNormals = true;
                    
                    if (oldNormals.Count <= 0)
                    {
                        haveNormals = false;
                    }

                    List<int> newTris = new List<int>();
                    List<Vector3> newVerts = new List<Vector3>();

                    List<Vector3> newNormals = new List<Vector3>();

                    List<Vector2> newUv1s = new List<Vector2>();
                    List<Vector2> newUv2s = new List<Vector2>();
                    List<Vector2> newUv3s = new List<Vector2>();
                    List<Vector2> newUv4s = new List<Vector2>();
                    
                    //Debug.Log("DEBUG: begin");

                    for (int j = 0; j < oldTris.Length; j++)
                    {
                        var oldTri = oldTris[j];
                        var oldVert = oldVerts[oldTri];
                        var oldNormal = new Vector3(0,0,0);
                        
                        if (haveNormals)
                        {
                            oldNormal = oldNormals[oldTri];
                        }

                        // First check if we already have a vert at the same position in the new mesh, re-use it if we do...
                        int found = vertReplacements[oldTri];

                        if (found >= 0)
                        {// Reuse the old vert...
                            newTris.Add(found);
                        }
                        else
                        {// Vert position not already in the list, add a new one...
                            newVerts.Add(oldVert);
                            
                            if (haveNormals) newNormals.Add(oldNormal);
                            
                            newTris.Add(newVerts.Count - 1);

                            if (uv1s.Count > 0 && uv1s.Count >= oldTri)
                            {
                                newUv1s.Add(uv1s[oldTri]);
                            }

                            if (uv2s.Count > 0 && uv2s.Count >= oldTri)
                            {
                                newUv2s.Add(uv2s[oldTri]);
                            }

                            if (uv3s.Count > 0 && uv3s.Count >= oldTri)
                            {
                                newUv3s.Add(uv3s[oldTri]);
                            }

                            if (uv4s.Count > 0 && uv4s.Count >= oldTri)
                            {
                                newUv4s.Add(uv4s[oldTri]);
                            }
                            
                            vertReplacements[oldTri] = newVerts.Count - 1;
                        }
                    }
                    
                    //Debug.Log("DEBUG: reged");

                    newMesh.SetVertices(newVerts);
                    newMesh.SetNormals(newNormals);
                    newMesh.SetTriangles(newTris, 0);
                    newMesh.SetUVs(0, newUv1s);
                    newMesh.SetUVs(1, newUv2s);
                    newMesh.SetUVs(2, newUv3s);
                    newMesh.SetUVs(3, newUv4s);
                    
                    //Debug.Log("DEBUG: newMesh");

                    var newGameObject = new GameObject();
                    //Debug.Log("DEBUG: createGO");

                    //newGameObject.transform.parent = combineParent.transform;
                    newGameObject.transform.parent = transform;
                    //Debug.Log("DEBUG: transform");
                    
                    var newMeshFilter = newGameObject.AddComponent<MeshFilter>();
                    var newMeshRenderer = newGameObject.AddComponent<MeshRenderer>();
                    //Debug.Log("DEBUG: create R+F");
                    
                    
                    newGameObject.name = new_mesh_name;
                    //Debug.Log("DEBUG: named");
        

                    Vector3 zero;
                    zero.x = 0.0f;
                    zero.y = 0.0f;
                    zero.z = 0.0f;

                    Vector3 scale;
                    scale.x = 1.0f;
                    scale.y = 1.0f;
                    scale.z = 1.0f;
                    newGameObject.transform.localPosition = zero;
                    newGameObject.transform.localRotation = Quaternion.identity;
                    newGameObject.transform.localScale = scale;

/*
                    newGameObject.transform.position = transform.position;
                    newGameObject.transform.rotation = transform.rotation;
                    //newGameObject.transform.scale = transform.scale;
*/
                    Material[] newMats = new Material[1];
                    newMats.SetValue(materials[i], 0);
                    newMeshRenderer.sharedMaterials = newMats;
                    newMeshFilter.sharedMesh = newMesh;
                    
                    //Debug.Log("DEBUG: newGOset");
                    
                    
                    newMeshRenderer.lightmapScaleOffset = meshRenderer.lightmapScaleOffset;
                    newMeshRenderer.realtimeLightmapScaleOffset = meshRenderer.realtimeLightmapScaleOffset;
                    newMeshRenderer.lightmapIndex = meshRenderer.lightmapIndex;
                    
                    //Debug.Log("DEBUG: lightmap");
                }
                
                //Debug.Log("MeshSplit:: Completed");
                
                meshRenderer.enabled = false;
            }
            catch (Exception e)
            {
                Debug.LogError("Exception caught: " + e);
            }
        }
        
        //
        // Check if we have access to the asset's meshes...
        //
        
        bool CheckMeshAccessability(GameObject combineParent)
        {
            // Verify there is existing object root, otherwise bail.
            if (combineParent == null)
            {
                Debug.Log("DrawCallOptimizer: Parent of objects to combne not assigned. Operation cancelled.");
                return false;
            }
                
            MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();
            
            // Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
            foreach (var meshFilter in meshFilters)
            {
                if (!meshFilter.sharedMesh || !meshFilter.sharedMesh.isReadable)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        //
        // Cleanup...
        //
        
        void CleanupDisabledRenderers(GameObject combineParent)
        {
            MeshRenderer[] meshRenderers = combineParent.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer r in meshRenderers)
            {
                if (!r.enabled)
                {
                    MeshFilter mf = r.gameObject.GetComponent<MeshFilter>();
                    DestroyImmediate(r, false);
                    
                    if (mf != null)
                        DestroyImmediate(mf, false);
                }
            }
        }
        
        void CleanupEmptyGameObjects(GameObject combineParent)
        {
            int numDestroyed = 0;

            bool modified = true;

            while (modified)
            {
                modified = false;

                Transform[] transforms = combineParent.GetComponentsInChildren<Transform>();

                foreach (Transform t in transforms)
                {
                    GameObject GO = t.gameObject;

                    Component[] components = GO.GetComponents(typeof(Component));

                    if (components.Length <= 1 && t.childCount <= 0)
                    {
                        DestroyImmediate(GO, false);
                        modified = true;
                        numDestroyed++;
                    }
                }
            }
            
            if (numDestroyed > 0)
            {
                Debug.Log("DrawCallOptimizer: Destroyed " + numDestroyed + " empty game objects.");
            }
        }
        
        //
        //
        //
        
        void CheckRecalculateNormals(GameObject combineParent)
        {
            try
            {
                int numMeshesFixed = 0;
                
                MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();

                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf.sharedMesh != null)
                    {// Check if it has normals, if not, build them now...
                        var oldNormals = new List<Vector3>();
                        mf.sharedMesh.GetNormals(oldNormals);
                        
                        bool haveNormals = true;
                        
                        if (oldNormals.Count <= 0)
                        {
                            haveNormals = false;
                        }
                        
                        if (!haveNormals)
                        {
                            mf.sharedMesh.RecalculateNormals();
                            mf.sharedMesh.RecalculateTangents();
                            numMeshesFixed++;
                        }
                    }
                }
                
                if (numMeshesFixed > 0)
                {
                    Debug.Log("DrawCallOptimizer: Generated missing mesh normals and tangents for " + numMeshesFixed + " meshes.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception caught: " + e);
            }
        }
        
        //
        // Fix missing materials...
        //
        
        void FixMissingMaterials(GameObject combineParent)
        {
            // Verify there is existing object root, otherwise bail.
            if (combineParent == null)
            {
                return;
            }
            
            //List<Transform> transforms = new List<Transform>();
            MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();
            
            // Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
            foreach (var meshFilter in meshFilters)
            {
                var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                
                if (meshRenderer == null)
                {
                    Debug.Log("DrawCallOptimizer: The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
                    continue;
                }
                
                if (!meshRenderer.enabled)
                {
                    continue;
                }
                
                var mats = meshRenderer.sharedMaterials;
                
                if (mats == null || mats.Length < 1)
                {
                    Material[] newMats = new Material[1];
                        
                    if (meshRenderer.sharedMaterial != null)
                    {
                        newMats[0] = meshRenderer.sharedMaterial;
                        Debug.Log("Material on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[0].name + ".");
                    }
                    else if (meshRenderer.material != null)
                    {
                        newMats[0] = meshRenderer.material;
                        Debug.Log("Material on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[0].name + ".");
                    }
                    else
                    {
                        newMats[0] = new Material(Shader.Find("Standard"));
                        Debug.Log("Material on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[0].name + ".");
                    }
                    
                    meshRenderer.sharedMaterials = newMats;
                    meshRenderer.sharedMaterial = null;
                    meshRenderer.material = null;
                }
                else
                {
                    Material[] newMats = new Material[mats.Length];
                
                    for (int k = 0; k < mats.Length; k++)
                    {
                        if (mats[k] == null)
                        {
                            if (meshRenderer.sharedMaterial != null)
                            {
                                newMats[k] = meshRenderer.sharedMaterial;
                                Debug.Log("Material " + k + " on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[k].name + ".");
                            }
                            else if (meshRenderer.material != null)
                            {
                                newMats[k] = meshRenderer.material;
                                Debug.Log("Material " + k + " on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[k].name + ".");
                            }
                            else
                            {
                                newMats[k] = new Material(Shader.Find("Standard"));
                                Debug.Log("Material " + k + " on mesh filter " + meshFilter.gameObject.name + " was missing and is now " + newMats[k].name + ".");
                            }
                        }
                        else
                        {
                            newMats[k] = mats[k];
                        }
                    }
                    
                    meshRenderer.sharedMaterials = newMats;
                }
            }
        }

    }
}
