using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    public abstract class GPUInstancerEditor : Editor
    {
        public static readonly float PROTOTYPE_RECT_SIZE = 80;
        public static readonly float PROTOTYPE_RECT_PADDING = 5;
        public static readonly Vector2 PROTOTYPE_RECT_PADDING_VECTOR = new Vector2(PROTOTYPE_RECT_PADDING, PROTOTYPE_RECT_PADDING);
        public static readonly Vector2 PROTOTYPE_RECT_SIZE_VECTOR = new Vector2(PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2, PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2);

        //protected SerializedProperty prop_settings;
        protected SerializedProperty prop_autoSelectCamera;
        protected SerializedProperty prop_mainCamera;
        protected SerializedProperty prop_renderOnlySelectedCamera;
        protected SerializedProperty prop_isManagerFrustumCulling;
        protected SerializedProperty prop_isManagerOcclusionCulling;
        protected SerializedProperty prop_minCullingDistance;

        protected bool showPrototypeBox = true;
        protected bool showAdvancedBox = false;
        protected bool showHelpText = false;
        protected Texture2D helpIcon;
        protected Texture2D helpIconActive;

        protected GUIContent[] prototypeContents = null;

        protected List<GPUInstancerPrototype> prototypeList;

        protected string wikiHash;

        private GameObject _redirectObject;

        // Previews
        private PreviewRenderUtility _previewRenderUtility;
        private GUIStyle _previewStyle = null;

        protected virtual void OnEnable()
        {
            prototypeContents = null;

            helpIcon = Resources.Load<Texture2D>(GPUInstancerConstants.EDITOR_TEXTURES_PATH + GPUInstancerEditorConstants.HELP_ICON);
            helpIconActive = Resources.Load<Texture2D>(GPUInstancerConstants.EDITOR_TEXTURES_PATH + GPUInstancerEditorConstants.HELP_ICON_ACTIVE);

            if (_previewRenderUtility == null)
                _previewRenderUtility = new PreviewRenderUtility();

            prop_autoSelectCamera = serializedObject.FindProperty("autoSelectCamera");
            prop_mainCamera = serializedObject.FindProperty("cameraData").FindPropertyRelative("mainCamera");
            prop_renderOnlySelectedCamera = serializedObject.FindProperty("cameraData").FindPropertyRelative("renderOnlySelectedCamera");
            prop_isManagerFrustumCulling = serializedObject.FindProperty("isFrustumCulling");
            prop_isManagerOcclusionCulling = serializedObject.FindProperty("isOcclusionCulling");
            prop_minCullingDistance = serializedObject.FindProperty("minCullingDistance");
        }

        protected virtual void OnDisable()
        {
            if (prototypeContents != null)
            {
                for (int i = 0; i < prototypeContents.Length; i++)
                {
                    if (prototypeContents[i] != null && prototypeContents[i].image != null)
                        DestroyImmediate(prototypeContents[i].image);
                }
            }
            prototypeContents = null;

            if (_previewRenderUtility != null)
                _previewRenderUtility.Cleanup();
        }

        public override void OnInspectorGUI()
        {
            if (_previewStyle == null)
            {
                _previewStyle = new GUIStyle("box");
                _previewStyle.normal.background = new Texture2D(1, 1);

                _previewStyle.normal.background.SetPixel(0, 0, new Color(82 / 255f, 82 / 255f, 82 / 255f, 1));
#if UNITY_2017
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    _previewStyle.normal.background.SetPixel(0, 0, new Color(82 / 255f, 82 / 255f, 82 / 255f, 1).gamma);
#endif

                _previewStyle.normal.background.Apply();
            }

            if (prototypeContents == null || prototypeList.Count != prototypeContents.Length)
                GeneratePrototypeContents();
            GPUInstancerEditorConstants.Styles.foldout.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal(GPUInstancerEditorConstants.Styles.box);
            EditorGUILayout.LabelField(GPUInstancerEditorConstants.GPUI_VERSION, GPUInstancerEditorConstants.Styles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawWikiButton(GUILayoutUtility.GetRect(40, 20), wikiHash);
            GUILayout.Space(10);
            DrawHelpButton(GUILayoutUtility.GetRect(20, 20), showHelpText);
            EditorGUILayout.EndHorizontal();
        }

        public virtual void InspectorGUIEnd()
        {
            if (_redirectObject != null)
            {
                Selection.activeGameObject = _redirectObject;
                _redirectObject = null;
            }
        }

        public virtual void FillPrototypeList() { }

        public void GeneratePrototypeContents()
        {
            FillPrototypeList();
            prototypeContents = new GUIContent[prototypeList.Count];
            if (prototypeList == null || prototypeList.Count == 0)
                return;
            for (int i = 0; i < prototypeList.Count; i++)
            {
                prototypeContents[i] = new GUIContent(GetPreview(prototypeList[i]), prototypeList[i].ToString());
            }
        }

        public Texture GetPreviewTexture(GPUInstancerPrototype prototype)
        {
            if (prototype.prefabObject == null)
                return null;

            if (prototype.prefabObject.GetComponentInChildren<MeshFilter>() == null)
                return null;

            try
            {
                Renderer[] renderers = prototype.prefabObject.GetComponentsInChildren<Renderer>();
                Bounds bounds = new Bounds();
                bool isBoundsInitialized = false;
                Bounds rendererBounds;
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.GetComponent<MeshFilter>())
                    {

                        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        if (mesh != null)
                        {
                            rendererBounds = renderer.bounds;// new Bounds(renderer.bounds.center + renderer.transform.position - prototype.prefabObject.transform.position, bounds.size);
                            if (!isBoundsInitialized)
                            {
                                isBoundsInitialized = true;
                                bounds = new Bounds(rendererBounds.center, rendererBounds.size);
                            }
                            else
                            {
                                bounds.Encapsulate(rendererBounds);
                            }
                        }
                    }
                }
                float maxBounds = Mathf.Max(Mathf.Max(bounds.extents.x, bounds.extents.y), bounds.extents.z);

#if UNITY_2017_1_OR_NEWER
                _previewRenderUtility.ambientColor = Color.gray;

#if UNITY_2017
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                    _previewRenderUtility.ambientColor = Color.gray.gamma;
#endif

                _previewRenderUtility.camera.transform.position = bounds.center;
                _previewRenderUtility.camera.transform.rotation = Quaternion.Euler(30, -135, 0);
                _previewRenderUtility.camera.farClipPlane = 100;
                _previewRenderUtility.camera.nearClipPlane = -100;
                _previewRenderUtility.camera.orthographic = true;
                _previewRenderUtility.camera.orthographicSize = maxBounds * 1.3f;

#if UNITY_2017
                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                {
                    _previewRenderUtility.lights[0].color = _previewRenderUtility.lights[0].color.gamma;
                    _previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(30, -135, 0);
                    _previewRenderUtility.lights[1].color = _previewRenderUtility.lights[1].color.gamma;
                    _previewRenderUtility.lights[1].transform.rotation = Quaternion.Euler(30, -135, 0) * Quaternion.Euler(340, 218, 177);
                }
#endif

#else
                _previewRenderUtility.m_Camera.transform.position = bounds.center;
                _previewRenderUtility.m_Camera.transform.rotation = Quaternion.Euler(30, -135, 0);
                _previewRenderUtility.m_Camera.farClipPlane = 100;
                _previewRenderUtility.m_Camera.nearClipPlane = -100;
                _previewRenderUtility.m_Camera.orthographic = true;
                _previewRenderUtility.m_Camera.orthographicSize = maxBounds * 1.3f;

#endif

                _previewRenderUtility.BeginPreview(new Rect(0, 0, PROTOTYPE_RECT_SIZE - 10, PROTOTYPE_RECT_SIZE - 10), _previewStyle);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.GetComponent<MeshFilter>())
                    {
                        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        if (mesh != null && renderer.sharedMaterials != null)
                        {
                            int submeshIndex = 0;
                            foreach (Material mat in renderer.sharedMaterials)
                            {
                                _previewRenderUtility.DrawMesh(mesh, renderer.transform.localToWorldMatrix, mat, mesh.subMeshCount == 1 ? 0 : submeshIndex);
                                submeshIndex++;
                            }
                        }
                    }
                }

#if UNITY_2017_1_OR_NEWER
                _previewRenderUtility.camera.Render();
#else
                _previewRenderUtility.m_Camera.Render();
#endif

                Texture preview = _previewRenderUtility.EndPreview();
                if (preview != null)
                {
                    // Copy preview texture so that if unity destroys it we have our own texture reference
#if UNITY_2017_1_OR_NEWER
                    Texture2D newTx = new Texture2D(preview.width, preview.height, TextureFormat.RGBAFloat, true, false);
#else
                    Texture2D newTx = new Texture2D(preview.width, preview.height);
#endif
                    if (preview is Texture2D)
                    {
                        newTx.SetPixels(((Texture2D)preview).GetPixels());
                        newTx.Apply();
                    }
                    else if (preview is RenderTexture)
                    {
                        RenderTexture previous = RenderTexture.active;
                        RenderTexture.active = (RenderTexture)preview;
                        newTx.ReadPixels(new Rect(0, 0, preview.width, preview.height), 0, 0);
                        newTx.Apply();
                        RenderTexture.active = previous;
                    }
                    return newTx;
                }
            }
            catch (Exception) { }
            return null;
        }

        public Texture2D GetPreviewTextureFromTexture2D(Texture2D texture)
        {
            if (!texture)
                return null;
            try
            {
                // Create a temporary RenderTexture of the same size as the texture
                RenderTexture tempRT = RenderTexture.GetTemporary(
                                    texture.width,
                                    texture.height,
                                    0,
                                    RenderTextureFormat.Default,
                                    RenderTextureReadWrite.Linear);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture, tempRT);
                // Backup the currently set RenderTexture
                RenderTexture previous = RenderTexture.active;
                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tempRT;
                // Create a new readable Texture2D to copy the pixels to it
#if UNITY_2017_1_OR_NEWER
                Texture2D myTexture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, true, false);
#else
                Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
#endif
                // Copy the pixels from the RenderTexture to the new Texture
                myTexture2D.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                myTexture2D.Apply();
                // Reset the active RenderTexture
                RenderTexture.active = previous;
                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tempRT);

                return myTexture2D;
            }
            catch (Exception) { }
            return null;
        }

        public Texture GetPreview(GPUInstancerPrototype prototype)
        {
            UnityEngine.Object previewObject = GetPreviewObject(prototype);

            if (previewObject == null)
                return null;

            //Texture2D preview = AssetPreview.GetAssetPreview(previewObject);
            //if (preview != null)
            //{
            //    // Copy preview texture so that if unity destroys it we have our own texture reference
            //    Texture2D newTx = new Texture2D(preview.width, preview.height);
            //    newTx.SetPixels(preview.GetPixels());
            //    newTx.Apply();
            //    return newTx;
            //}
            if (previewObject is Texture2D)
                return GetPreviewTextureFromTexture2D((Texture2D)previewObject);
            return GetPreviewTexture(prototype);
        }

        public static UnityEngine.Object GetPreviewObject(GPUInstancerPrototype prototype)
        {
            UnityEngine.Object previewObject = prototype.prefabObject;
            if (prototype is GPUInstancerDetailPrototype)
            {
                if (((GPUInstancerDetailPrototype)prototype).prototypeTexture != null)
                    previewObject = ((GPUInstancerDetailPrototype)prototype).prototypeTexture;
            }

            return previewObject;
        }

        public void DrawCameraDataFields()
        {
            EditorGUILayout.PropertyField(prop_autoSelectCamera);
            if (!prop_autoSelectCamera.boolValue)
                EditorGUILayout.PropertyField(prop_mainCamera, GPUInstancerEditorConstants.Contents.useCamera);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_camera);
            EditorGUILayout.PropertyField(prop_renderOnlySelectedCamera, GPUInstancerEditorConstants.Contents.renderOnlySelectedCamera);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_renderOnlySelectedCamera);
        }

        public void DrawCullingSettings(List<GPUInstancerPrototype> protoypeList)
        {
            EditorGUILayout.PropertyField(prop_isManagerFrustumCulling, GPUInstancerEditorConstants.Contents.useManagerFrustumCulling);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_managerFrustumCulling);
            EditorGUILayout.PropertyField(prop_isManagerOcclusionCulling, GPUInstancerEditorConstants.Contents.useManagerOcclusionCulling);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_managerOcclusionCulling);

            // Min Culling Distance
            Rect pRect = GUILayoutUtility.GetRect(GPUInstancerEditorConstants.Contents.minManagerCullingDistance, GUI.skin.horizontalSlider);
            GUIContent label = EditorGUI.BeginProperty(pRect, GPUInstancerEditorConstants.Contents.minManagerCullingDistance, prop_minCullingDistance);
            EditorGUI.BeginChangeCheck();
            var newCullingDistanceValue = EditorGUI.Slider(pRect, label, prop_minCullingDistance.floatValue, 0, 100);

            if (EditorGUI.EndChangeCheck())
            {
                if (protoypeList != null)
                {
                    foreach (GPUInstancerPrototype prototype in protoypeList)
                    {
                        if (prototype.minCullingDistance == prop_minCullingDistance.floatValue)
                        {
                            prototype.minCullingDistance = newCullingDistanceValue;
                            EditorUtility.SetDirty(prototype);
                        }
                    }
                }
                prop_minCullingDistance.floatValue = newCullingDistanceValue;
            }
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_minCullingDistance);

            if (protoypeList != null)
            {
                foreach (GPUInstancerPrototype prototype in protoypeList)
                {
                    if (prototype.minCullingDistance < newCullingDistanceValue)
                    {
                        prototype.minCullingDistance = newCullingDistanceValue;
                        EditorUtility.SetDirty(prototype);
                    }
                }
            }
        }

        public void DrawSceneSettingsBox()
        {
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(GPUInstancerEditorConstants.TEXT_sceneSettings, GPUInstancerEditorConstants.Styles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DrawSettingContents();

            EditorGUILayout.EndVertical();
        }

        public abstract void DrawSettingContents();

        public virtual void DrawGPUInstancerPrototypeButton(GPUInstancerPrototype prototype, GUIContent prototypeContent, bool isSelected, UnityAction handleSelect)
        {
            if (prototypeContent.image == null)
                prototypeContent.image = GetPreview(prototype);

            Rect prototypeRect = GUILayoutUtility.GetRect(PROTOTYPE_RECT_SIZE, PROTOTYPE_RECT_SIZE, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            Rect iconRect = new Rect(prototypeRect.position + new Vector2(PROTOTYPE_RECT_PADDING, PROTOTYPE_RECT_PADDING),
                new Vector2(PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2, PROTOTYPE_RECT_SIZE - PROTOTYPE_RECT_PADDING * 2));

            GUI.SetNextControlName(prototypeContent.tooltip);
            if (isSelected)
            {
                GPUInstancerEditorConstants.DrawColoredButton(prototypeContent,
                    string.IsNullOrEmpty(prototype.warningText) ? GPUInstancerEditorConstants.Colors.lightGreen : GPUInstancerEditorConstants.Colors.lightred, Color.black,
                    FontStyle.Normal, iconRect, null);
            }
            else
            {
                GPUInstancerEditorConstants.DrawColoredButton(prototypeContent,
                    string.IsNullOrEmpty(prototype.warningText) ? GUI.backgroundColor : GPUInstancerEditorConstants.Colors.darkred, Color.black,
                    FontStyle.Normal, iconRect,
                    () =>
                    {
                        if (handleSelect != null)
                            handleSelect();
                    });
            }
        }

        public virtual void DrawGPUInstancerPrototypeBox(GPUInstancerPrototype selectedPrototype, bool isFrustumCulling, bool isOcclusionCulling,
            GPUInstancerShaderBindings shaderBindings, GPUInstancerBillboardAtlasBindings billboardAtlasBindings)
        {
            if (selectedPrototype == null)
                return;

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            // title
            Rect foldoutRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            foldoutRect.x += 12;
            showPrototypeBox = EditorGUI.Foldout(foldoutRect, showPrototypeBox, selectedPrototype.ToString(), true, GPUInstancerEditorConstants.Styles.foldout);

            if (!showPrototypeBox)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            if (!string.IsNullOrEmpty(selectedPrototype.warningText))
            {
                EditorGUILayout.HelpBox(selectedPrototype.warningText, MessageType.Error);
                if (selectedPrototype.warningText.StartsWith("Can not create instanced version for shader"))
                {
                    GPUInstancerEditorConstants.DrawColoredButton(new GUIContent("Go to Unity Archive"),
                        GPUInstancerEditorConstants.Colors.lightred, Color.white, FontStyle.Bold, Rect.zero,
                        () =>
                        {
                            Application.OpenURL("https://unity3d.com/get-unity/download/archive");
                        });
                    GUILayout.Space(10);
                }
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_prefabObject, selectedPrototype.prefabObject, typeof(GameObject), false);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_prototypeSO, selectedPrototype, typeof(GPUInstancerPrototype), false);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();

            #region Shadows
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_shadows, GPUInstancerEditorConstants.Styles.boldLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            selectedPrototype.isShadowCasting = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isShadowCasting, selectedPrototype.isShadowCasting);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isShadowCasting);
            EditorGUI.EndDisabledGroup();
            if (selectedPrototype.isShadowCasting)
            {
                if (selectedPrototype is GPUInstancerPrefabPrototype)
                {
                    EditorGUI.BeginDisabledGroup(Application.isPlaying);
                    selectedPrototype.useOriginalShaderForShadow = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useOriginalShaderForShadow, selectedPrototype.useOriginalShaderForShadow);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useOriginalShaderForShadow);
                    EditorGUI.EndDisabledGroup();
                }

                if (!(selectedPrototype is GPUInstancerDetailPrototype))
                {
                    selectedPrototype.useCustomShadowDistance = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useCustomShadowDistance, selectedPrototype.useCustomShadowDistance);
                    if (selectedPrototype.useCustomShadowDistance)
                    {
                        selectedPrototype.shadowDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_shadowDistance, selectedPrototype.shadowDistance, 0.0f, QualitySettings.shadowDistance);
                    }
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useCustomShadowDistance);
                    if (selectedPrototype.prefabObject != null && selectedPrototype.prefabObject.GetComponent<LODGroup>() != null)
                    {
                        LODGroup lodGroup = selectedPrototype.prefabObject.GetComponent<LODGroup>();
                        List<GUIContent> optionsList = GPUInstancerEditorConstants.Contents.LODs.GetRange(0, lodGroup.lodCount);
                        optionsList.Add(GPUInstancerEditorConstants.Contents.LODs[8]);
                        GUIContent[] options = optionsList.ToArray();
                        int index = 0;
                        for (int i = 0; i < lodGroup.lodCount; i++)
                        {
                            index = i * 4;
                            if (i >= 4)
                                index = (i - 4) * 4 + 1;
                            selectedPrototype.shadowLODMap[index] = EditorGUILayout.Popup(GPUInstancerEditorConstants.Contents.shadowLODs[i], selectedPrototype.shadowLODMap[index],
                                options);
                        }
                    }

                    selectedPrototype.cullShadows = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_cullShadows, selectedPrototype.cullShadows);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_cullShadows);
                }
            }

            EditorGUILayout.EndVertical();
            #endregion Shadows

            #region Culling
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_culling, GPUInstancerEditorConstants.Styles.boldLabel);

            selectedPrototype.maxDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_maxDistance, selectedPrototype.maxDistance, 0.0f, GetMaxDistance(selectedPrototype));
            DrawHelpText(selectedPrototype is GPUInstancerDetailPrototype ? GPUInstancerEditorConstants.HELPTEXT_maxDistanceDetail : GPUInstancerEditorConstants.HELPTEXT_maxDistance);
            if (isFrustumCulling)
            {
                selectedPrototype.isFrustumCulling = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isFrustumCulling, selectedPrototype.isFrustumCulling);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isFrustumCulling);
                selectedPrototype.frustumOffset = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_frustumOffset, selectedPrototype.frustumOffset, 0.0f, 0.5f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_frustumOffset);
            }

            if (isOcclusionCulling)
            {
                selectedPrototype.isOcclusionCulling = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isOcclusionCulling, selectedPrototype.isOcclusionCulling);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isOcclusionCulling);
            }

            if (isFrustumCulling || isOcclusionCulling)
            {
                selectedPrototype.minCullingDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_minCullingDistance, selectedPrototype.minCullingDistance, 0, 100);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_minCullingDistance);
            }

            EditorGUILayout.EndVertical();
            #endregion Culling

            #region LOD
            if (selectedPrototype.prefabObject != null && (selectedPrototype.prefabObject.GetComponent<LODGroup>() != null || selectedPrototype.useGeneratedBillboard))
            {
                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_LOD, GPUInstancerEditorConstants.Styles.boldLabel);

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                selectedPrototype.isLODCrossFade = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isLODCrossFade, selectedPrototype.isLODCrossFade);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isLODCrossFade);

                if (selectedPrototype.isLODCrossFade)
                {
                    selectedPrototype.isLODCrossFadeAnimate = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isLODCrossFadeAnimate, selectedPrototype.isLODCrossFadeAnimate);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isLODCrossFadeAnimate);

                    if (!selectedPrototype.isLODCrossFadeAnimate)
                    {
                        selectedPrototype.lodFadeTransitionWidth = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_lodFadeTransitionWidth, selectedPrototype.lodFadeTransitionWidth, 0.0f, 1.0f);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_lodFadeTransitionWidth);
                    }
                }

                selectedPrototype.lodBiasAdjustment = EditorGUILayout.FloatField(GPUInstancerEditorConstants.TEXT_lodBiasAdjustment, selectedPrototype.lodBiasAdjustment);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_lodBiasAdjustment);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndVertical();
            }
            #endregion LOD

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            DrawGPUInstancerPrototypeInfo(selectedPrototype);

            DrawGPUInstancerPrototypeBillboardSettings(selectedPrototype, shaderBindings, billboardAtlasBindings);

            DrawGPUInstancerPrototypeActions();
            DrawGPUInstancerPrototypeAdvancedActions();

            if (EditorGUI.EndChangeCheck() && selectedPrototype != null)
            {
                EditorUtility.SetDirty(selectedPrototype);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        public virtual void DrawGPUInstancerPrototypeBillboardSettings(GPUInstancerPrototype selectedPrototype,
            GPUInstancerShaderBindings shaderBindings, GPUInstancerBillboardAtlasBindings billboardAtlasBindings)
        {
            if (selectedPrototype.isBillboardDisabled || (selectedPrototype is GPUInstancerDetailPrototype && !((GPUInstancerDetailPrototype)selectedPrototype).usePrototypeMesh))
            {
                if (selectedPrototype.useGeneratedBillboard)
                    selectedPrototype.useGeneratedBillboard = false;
                if (selectedPrototype.billboard != null)
                    selectedPrototype.billboard = null;
                return;
            }

            if (Event.current.type == EventType.Repaint && !selectedPrototype.checkedForBillboardExtentions)
            {
                selectedPrototype.checkedForBillboardExtentions = true;
                if (CheckForBillboardExtentions(selectedPrototype, billboardAtlasBindings))
                    return;
            }

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);

            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_billboardSettings, GPUInstancerEditorConstants.Styles.boldLabel);

            selectedPrototype.useGeneratedBillboard = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useGeneratedBillboard, selectedPrototype.useGeneratedBillboard);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useGeneratedBillboard);

            if (selectedPrototype.useGeneratedBillboard && selectedPrototype.billboard == null)
                selectedPrototype.billboard = new GPUInstancerBillboard();
            else if (!selectedPrototype.useGeneratedBillboard && selectedPrototype.billboard != null)
            {
                if (selectedPrototype.billboard.albedoAtlasTexture != null)
                    billboardAtlasBindings.DeleteBillboardTextures(selectedPrototype);
                if (!selectedPrototype.billboard.useCustomBillboard)
                    selectedPrototype.billboard = null;
            }

            if (selectedPrototype.useGeneratedBillboard)
            {
                if (selectedPrototype.treeType != GPUInstancerTreeType.SpeedTree && selectedPrototype.treeType != GPUInstancerTreeType.TreeCreatorTree && selectedPrototype.treeType != GPUInstancerTreeType.SoftOcclusionTree
                    && !selectedPrototype.billboard.useCustomBillboard)
                    EditorGUILayout.HelpBox(GPUInstancerEditorConstants.HELPTEXT_unsupportedBillboardWarning, MessageType.Warning);

                bool previousUseCustomBillboard = selectedPrototype.billboard.useCustomBillboard;
                selectedPrototype.billboard.useCustomBillboard = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useCustomBillboard, selectedPrototype.billboard.useCustomBillboard);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useCustomBillboard);

                if (selectedPrototype.billboard.useCustomBillboard)
                {
                    selectedPrototype.billboard.customBillboardMesh = (Mesh)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_customBillboardMesh,
                        selectedPrototype.billboard.customBillboardMesh, typeof(Mesh), false);
                    selectedPrototype.billboard.customBillboardMaterial = (Material)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_customBillboardMaterial,
                        selectedPrototype.billboard.customBillboardMaterial, typeof(Material), false);
                    selectedPrototype.billboard.isBillboardShadowCasting = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isBillboardShadowCasting,
                        selectedPrototype.billboard.isBillboardShadowCasting);

                    if (!previousUseCustomBillboard && selectedPrototype.billboard.albedoAtlasTexture != null)
                        billboardAtlasBindings.DeleteBillboardTextures(selectedPrototype);


                    if (shaderBindings != null && selectedPrototype.billboard.customBillboardMaterial != null)
                    {
                        if (!shaderBindings.IsShadersInstancedVersionExists(selectedPrototype.billboard.customBillboardMaterial.shader.name))
                        {
                            Shader instancedShader = GPUInstancerUtility.CreateInstancedShader(selectedPrototype.billboard.customBillboardMaterial.shader, shaderBindings);
                            if (instancedShader != null)
                                shaderBindings.AddShaderInstance(selectedPrototype.billboard.customBillboardMaterial.shader.name, instancedShader);
                        }
                    }
                }
                else
                {
                    if (selectedPrototype.billboard.customBillboardInLODGroup)
                        selectedPrototype.billboard.customBillboardInLODGroup = false;

                    selectedPrototype.billboard.billboardQuality = (BillboardQuality)EditorGUILayout.Popup(GPUInstancerEditorConstants.TEXT_billboardQuality,
                        (int)selectedPrototype.billboard.billboardQuality, GPUInstancerEditorConstants.TEXT_BillboardQualityOptions);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardQuality);

                    switch (selectedPrototype.billboard.billboardQuality)
                    {
                        case BillboardQuality.Low:
                            selectedPrototype.billboard.atlasResolution = 1024;
                            break;
                        case BillboardQuality.Mid:
                            selectedPrototype.billboard.atlasResolution = 2048;
                            break;
                        case BillboardQuality.High:
                            selectedPrototype.billboard.atlasResolution = 4096;
                            break;
                        case BillboardQuality.VeryHigh:
                            selectedPrototype.billboard.atlasResolution = 8192;
                            break;
                    }

                    selectedPrototype.billboard.frameCount = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_billboardFrameCount, selectedPrototype.billboard.frameCount, 8, 32);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardFrameCount);
                    selectedPrototype.billboard.frameCount = Mathf.NextPowerOfTwo(selectedPrototype.billboard.frameCount);

                    selectedPrototype.billboard.billboardBrightness = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_billboardBrightness, selectedPrototype.billboard.billboardBrightness, 0.0f, 1.0f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardBrightness);

                    selectedPrototype.billboard.isOverridingOriginalCutoff = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_overrideOriginalCutoff, selectedPrototype.billboard.isOverridingOriginalCutoff);
                    if (selectedPrototype.billboard.isOverridingOriginalCutoff)
                        selectedPrototype.billboard.cutoffOverride = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_overrideCutoffAmount, selectedPrototype.billboard.cutoffOverride, 0.01f, 1.0f);
                    else
                        selectedPrototype.billboard.cutoffOverride = -1f;
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_overrideOriginalCutoff);
                }

                if (!selectedPrototype.billboard.customBillboardInLODGroup)
                {
                    bool hasLODGroup = selectedPrototype.prefabObject.GetComponent<LODGroup>() != null;
                    bool speedTreeBillboard = selectedPrototype.treeType == GPUInstancerTreeType.SpeedTree && hasLODGroup
                        && selectedPrototype.prefabObject.GetComponentInChildren<BillboardRenderer>() != null;
                    if (hasLODGroup && !speedTreeBillboard)
                    {
                        selectedPrototype.billboard.replaceLODCullWithBillboard = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_replaceLODCull, selectedPrototype.billboard.replaceLODCullWithBillboard);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_replaceLODCull);
                    }
                    if ((!hasLODGroup || !selectedPrototype.billboard.replaceLODCullWithBillboard) && !speedTreeBillboard)
                    {
                        selectedPrototype.billboard.billboardDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_generatedBillboardDistance, selectedPrototype.billboard.billboardDistance, 0.01f, 1f);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_generatedBillboardDistance);
                    }
                }

                if (!selectedPrototype.billboard.useCustomBillboard)
                {
                    selectedPrototype.billboard.billboardFaceCamPos = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_billboardFaceCamPos, selectedPrototype.billboard.billboardFaceCamPos);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardFaceCamPos);

                    if (selectedPrototype.billboard.albedoAtlasTexture == null)
                        GPUInstancerUtility.AssignBillboardBinding(selectedPrototype, billboardAtlasBindings);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_billboardAlbedo, selectedPrototype.billboard.albedoAtlasTexture, typeof(GameObject), false);
                    EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_billboardNormal, selectedPrototype.billboard.normalAtlasTexture, typeof(GameObject), false);
                    EditorGUI.EndDisabledGroup();
                }

                GUILayout.Space(10);

                EditorGUILayout.BeginHorizontal();

                if (!selectedPrototype.billboard.useCustomBillboard)
                {
                    GPUInstancerEditorConstants.DrawColoredButton(selectedPrototype.billboard.albedoAtlasTexture == null ?
                        GPUInstancerEditorConstants.Contents.generateBillboard : GPUInstancerEditorConstants.Contents.regenerateBillboard,
                        GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                        () =>
                        {
                            GPUInstancerUtility.GeneratePrototypeBillboard(selectedPrototype, billboardAtlasBindings, selectedPrototype.billboard.albedoAtlasTexture != null);
                        });
                }

                if ((!selectedPrototype.billboard.useCustomBillboard && selectedPrototype.billboard.albedoAtlasTexture != null)
                    || (selectedPrototype.billboard.useCustomBillboard
                            && selectedPrototype.billboard.customBillboardMesh != null
                            && selectedPrototype.billboard.customBillboardMaterial != null))
                {
                    GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.showBillboard, GPUInstancerEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, Rect.zero,
                    () =>
                    {
                        GPUInstancerUtility.ShowBillboardQuad(selectedPrototype, Vector3.zero);
                    });
                }

                EditorGUILayout.EndHorizontal();

                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_regenerateBillboard);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_showBillboard);
            }

            if (selectedPrototype.useGeneratedBillboard && selectedPrototype.billboard != null && selectedPrototype.billboard.useCustomBillboard && GPUInstancerDefines.billboardExtentions != null && GPUInstancerDefines.billboardExtentions.Count > 0)
            {
                GUILayout.Space(10);

                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);

                GPUInstancerEditorConstants.DrawCustomLabel("External Billboard Generators", GPUInstancerEditorConstants.Styles.boldLabel);

                GUILayout.Space(5);

                foreach (Extention.GPUInstancerBillboardExtention billboardExtention in GPUInstancerDefines.billboardExtentions)
                {
                    try
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(billboardExtention.GetTitle(), GPUInstancerEditorConstants.Styles.label);

                        GPUInstancerEditorConstants.DrawColoredButton(new GUIContent(billboardExtention.GetButtonText()), GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                        () =>
                        {
                            _redirectObject = billboardExtention.GenerateBillboard(selectedPrototype.prefabObject);
                            selectedPrototype.checkedForBillboardExtentions = false;
                        });
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(5);
                    }
                    catch (System.Exception e)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogError("Error generating billboard: " + e.Message + " StackTrace:" + e.StackTrace);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        public void DrawHelpText(string text, bool forceShow = false)
        {
            if (showHelpText || forceShow)
            {
                EditorGUILayout.HelpBox(text, MessageType.Info);
            }
        }

        public static void DrawWikiButton(Rect buttonRect, string hash)
        {
            GPUInstancerEditorConstants.DrawColoredButton(new GUIContent("Wiki"),
                    GPUInstancerEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, buttonRect,
                    () => { Application.OpenURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer:GettingStarted" + hash); }
                    );
        }

        public void DrawHelpButton(Rect buttonRect, bool showingHelp)
        {
            if (GUI.Button(buttonRect, new GUIContent(showHelpText ? helpIconActive : helpIcon,
                showHelpText ? GPUInstancerEditorConstants.TEXT_hideHelpTooltip : GPUInstancerEditorConstants.TEXT_showHelpTooltip), showHelpText ? GPUInstancerEditorConstants.Styles.helpButtonSelected : GPUInstancerEditorConstants.Styles.helpButton))
            {
                showHelpText = !showHelpText;
            }
        }

        public abstract void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype);
        public abstract void DrawGPUInstancerPrototypeActions();
        public virtual void DrawGPUInstancerPrototypeAdvancedActions() { }
        public abstract float GetMaxDistance(GPUInstancerPrototype selectedPrototype);

        public static bool CheckForBillboardExtentions(GPUInstancerPrototype selectedPrototype, GPUInstancerBillboardAtlasBindings billboardAtlasBindings)
        {
            bool hasExtentionBillboard = false;
            if (GPUInstancerDefines.billboardExtentions != null && GPUInstancerDefines.billboardExtentions.Count > 0)
            {
                foreach (Extention.GPUInstancerBillboardExtention billboardExtention in GPUInstancerDefines.billboardExtentions)
                {
                    try
                    {
                        if (billboardExtention.IsBillboardAdded(selectedPrototype.prefabObject))
                        {
                            Mesh generatedMesh = billboardExtention.GetBillboardMesh(selectedPrototype.prefabObject);
                            Material generatedMaterial = billboardExtention.GetBillboardMaterial(selectedPrototype.prefabObject);
                            bool isInLODGroup = billboardExtention.IsInLODGroup(selectedPrototype.prefabObject);
                            if (generatedMesh != null && generatedMaterial != null)
                            {
                                if (selectedPrototype.billboard == null)
                                    selectedPrototype.billboard = new GPUInstancerBillboard();

                                selectedPrototype.useGeneratedBillboard = true;
                                selectedPrototype.billboard.useCustomBillboard = true;
                                selectedPrototype.billboard.customBillboardInLODGroup = isInLODGroup;
                                selectedPrototype.billboard.customBillboardMesh = generatedMesh;
                                selectedPrototype.billboard.customBillboardMaterial = generatedMaterial;

                                hasExtentionBillboard = true;
                                break;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogError("Error generating billboard: " + e.Message + " StackTrace:" + e.StackTrace);
                    }
                }
            }
            return hasExtentionBillboard;
        }
    }
}
