using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerPrefabManager))]
    [CanEditMultipleObjects]
    public class GPUInstancerPrefabManagerEditor : GPUInstancerManagerEditor
    {
        private GPUInstancerPrefabManager _prefabManager;

        protected override void OnEnable()
        {
            base.OnEnable();

            wikiHash = "#The_Prefab_Manager";

            _prefabManager = (target as GPUInstancerPrefabManager);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            DrawSceneSettingsBox();

            DrawPrefabGlobalInfoBox();

            DrawGPUInstancerManagerGUILayout();

            HandlePickerObjectSelection();

            serializedObject.ApplyModifiedProperties();

            base.InspectorGUIEnd();
        }

        public override void DrawSettingContents()
        {
            EditorGUILayout.Space();

            DrawCameraDataFields();

            DrawCullingSettings(_prefabManager.prototypeList);

        }

        public void DrawPrefabGlobalInfoBox()
        {
            //if (_prefabManager.prefabList == null)
            //    return;

            //EditorGUI.BeginDisabledGroup(Application.isPlaying);
            //EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            //GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prefabGlobal, GPUInstancerEditorConstants.Styles.boldLabel);

            //EditorGUILayout.EndVertical();
            //EditorGUI.EndDisabledGroup();
        }

        public static void SetRenderersEnabled(GPUInstancerPrefabPrototype prefabPrototype, bool enabled)
        {
#if UNITY_2018_3_OR_NEWER
            GameObject prefabContents = GPUInstancerUtility.LoadPrefabContents(prefabPrototype.prefabObject);
#else
            GameObject prefabContents = prefabPrototype.prefabObject;
#endif
            MeshRenderer[] meshRenderers = prefabContents.GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers != null && meshRenderers.Length > 0)
                for (int mr = 0; mr < meshRenderers.Length; mr++)
                    meshRenderers[mr].enabled = enabled;

            BillboardRenderer[] billboardRenderers = prefabContents.GetComponentsInChildren<BillboardRenderer>(true);
            if (billboardRenderers != null && billboardRenderers.Length > 0)
                for (int mr = 0; mr < billboardRenderers.Length; mr++)
                    billboardRenderers[mr].enabled = enabled;

            LODGroup lodGroup = prefabContents.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.enabled = enabled;

            if (prefabPrototype.hasRigidBody)
            {
                Rigidbody rigidbody = prefabContents.GetComponent<Rigidbody>();

                if (enabled || prefabPrototype.autoUpdateTransformData)
                {
                    if (rigidbody == null)
                    {
                        GPUInstancerPrefabPrototype.RigidbodyData rigidbodyData = prefabPrototype.rigidbodyData;
                        if (rigidbodyData != null)
                        {
                            rigidbody = prefabPrototype.prefabObject.AddComponent<Rigidbody>();
                            rigidbody.useGravity = rigidbodyData.useGravity;
                            rigidbody.angularDrag = rigidbodyData.angularDrag;
                            rigidbody.mass = rigidbodyData.mass;
                            rigidbody.constraints = rigidbodyData.constraints;
                            rigidbody.detectCollisions = true;
                            rigidbody.drag = rigidbodyData.drag;
                            rigidbody.isKinematic = rigidbodyData.isKinematic;
                            rigidbody.interpolation = rigidbodyData.interpolation;
                        }
                    }
                }
                else if (rigidbody != null && !prefabPrototype.autoUpdateTransformData)
                    DestroyImmediate(rigidbody, true);
            }

#if UNITY_2018_3_OR_NEWER
            GPUInstancerUtility.UnloadPrefabContents(prefabPrototype.prefabObject, prefabContents, true);
#endif
            EditorUtility.SetDirty(prefabPrototype.prefabObject);
            prefabPrototype.meshRenderersDisabled = !enabled;
            EditorUtility.SetDirty(prefabPrototype);
        }

        public void DrawGPUInstancerManagerGUILayout()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            DrawRegisterPrefabsBox();
            EditorGUI.EndDisabledGroup();

            int prototypeRowCount = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / PROTOTYPE_RECT_SIZE);

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prototypes, GPUInstancerEditorConstants.Styles.boldLabel);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_prototypes);

            int i = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (GPUInstancerPrefabPrototype prototype in _prefabManager.prototypeList)
            {
                if (prototype == null)
                    continue;

                CheckPrefabRigidbodies(prototype);

                if (i != 0 && i % prototypeRowCount == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }

                DrawGPUInstancerPrototypeButton(prototype, prototypeContents[i]);
                i++;
            }

            if (i != 0 && i % prototypeRowCount == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
            if (!Application.isPlaying)
                DrawGPUInstancerPrototypeAddButton();

            EditorGUILayout.EndHorizontal();

            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addprototypeprefab);

            DrawGPUInstancerPrototypeBox(_prefabManager.selectedPrototype, _prefabManager.isFrustumCulling, _prefabManager.isOcclusionCulling,
                _prefabManager.shaderBindings, _prefabManager.billboardAtlasBindings);

            EditorGUILayout.EndVertical();
        }

        public static void CheckPrefabRigidbodies(GPUInstancerPrefabPrototype prototype)
        {
            if (prototype.prefabObject != null && !prototype.meshRenderersDisabled)
            {
                EditorGUI.BeginChangeCheck();
                Rigidbody rigidbody = prototype.prefabObject.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    prototype.hasRigidBody = true;
                    if (prototype.rigidbodyData == null)
                        prototype.rigidbodyData = new GPUInstancerPrefabPrototype.RigidbodyData();
                    prototype.rigidbodyData.useGravity = rigidbody.useGravity;
                    prototype.rigidbodyData.angularDrag = rigidbody.angularDrag;
                    prototype.rigidbodyData.mass = rigidbody.mass;
                    prototype.rigidbodyData.constraints = rigidbody.constraints;
                    prototype.rigidbodyData.drag = rigidbody.drag;
                    prototype.rigidbodyData.isKinematic = rigidbody.isKinematic;
                    prototype.rigidbodyData.interpolation = rigidbody.interpolation;
                }
                else
                {
                    prototype.hasRigidBody = false;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(prototype);
                }
            }
        }

        public void DrawRegisterPrefabsBox()
        {
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_registeredPrefabs, GPUInstancerEditorConstants.Styles.boldLabel);
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.registerPrefabsInScene, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    Undo.RecordObject(_prefabManager, "Register prefabs in scene");
                    _prefabManager.RegisterPrefabsInScene();
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_registerPrefabsInScene);

            if (!Application.isPlaying && _prefabManager.registeredPrefabs.Count > 0)
            {
                foreach (RegisteredPrefabsData rpd in _prefabManager.registeredPrefabs)
                {
                    GPUInstancerEditorConstants.DrawCustomLabel(rpd.prefabPrototype.ToString() + " Instance Count: " +
                        rpd.registeredPrefabs.Count,
                        GPUInstancerEditorConstants.Styles.label, false);
                }
            }
            else if (Application.isPlaying && _prefabManager.GetRegisteredPrefabsRuntimeData() != null && _prefabManager.GetRegisteredPrefabsRuntimeData().Count > 0)
            {
                foreach (GPUInstancerPrototype p in _prefabManager.GetRegisteredPrefabsRuntimeData().Keys)
                {
                    GPUInstancerEditorConstants.DrawCustomLabel(p.ToString() + " Instance Count: " +
                        _prefabManager.GetRegisteredPrefabsRuntimeData()[p].Count,
                        GPUInstancerEditorConstants.Styles.label, false);
                }
            }
            else
                GPUInstancerEditorConstants.DrawCustomLabel("No registered prefabs.", GPUInstancerEditorConstants.Styles.label, false);

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype)
        {
            DrawGPUInstancerPrototypeInfo(selectedPrototype, (string t) => { DrawHelpText(t); });
        }

        public static void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype, UnityAction<string> DrawHelpText)
        {
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prefabRuntimeSettings, GPUInstancerEditorConstants.Styles.boldLabel);

            GPUInstancerPrefabPrototype prototype = (GPUInstancerPrefabPrototype)selectedPrototype;
            prototype.enableRuntimeModifications = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_enableRuntimeModifications, prototype.enableRuntimeModifications);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_enableRuntimeModifications);

            EditorGUI.BeginDisabledGroup(!prototype.enableRuntimeModifications);

            EditorGUI.BeginDisabledGroup(!prototype.hasRigidBody || prototype.autoUpdateTransformData);
            prototype.startWithRigidBody = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_startWithRigidBody, prototype.startWithRigidBody);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_startWithRigidBody);
            EditorGUI.EndDisabledGroup();

            prototype.addRemoveInstancesAtRuntime = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_addRemoveInstancesAtRuntime, prototype.addRemoveInstancesAtRuntime);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addRemoveInstancesAtRuntime);

            EditorGUI.BeginDisabledGroup(!prototype.addRemoveInstancesAtRuntime);
            prototype.extraBufferSize = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_extraBufferSize, prototype.extraBufferSize, 0, GPUInstancerConstants.PREFAB_EXTRA_BUFFER_SIZE);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_extraBufferSize);

            prototype.addRuntimeHandlerScript = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_addRuntimeHandlerScript, prototype.addRuntimeHandlerScript);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addRuntimeHandlerScript);

            if (prototype.addRemoveInstancesAtRuntime && !Application.isPlaying)
            {
                GPUInstancerPrefabRuntimeHandler prefabRuntimeHandler = prototype.prefabObject.GetComponent<GPUInstancerPrefabRuntimeHandler>();
                if (prototype.addRuntimeHandlerScript && prefabRuntimeHandler == null)
                {
#if UNITY_2018_3_OR_NEWER
                    GPUInstancerUtility.AddComponentToPrefab<GPUInstancerPrefabRuntimeHandler>(prototype.prefabObject);
#else
                    prototype.prefabObject.AddComponent<GPUInstancerPrefabRuntimeHandler>();
#endif
                    EditorUtility.SetDirty(prototype.prefabObject);
                }
                else if (!prototype.addRuntimeHandlerScript && prefabRuntimeHandler != null)
                {
#if UNITY_2018_3_OR_NEWER
                    GPUInstancerUtility.RemoveComponentFromPrefab<GPUInstancerPrefabRuntimeHandler>(prototype.prefabObject);
#else
                    DestroyImmediate(prefabRuntimeHandler, true);
#endif
                    EditorUtility.SetDirty(prototype.prefabObject);
                }
            }
            EditorGUI.EndDisabledGroup();

            bool autoUpdateTransformData = prototype.autoUpdateTransformData;
            prototype.autoUpdateTransformData = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_autoUpdateTransformData, prototype.autoUpdateTransformData);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_autoUpdateTransformData);
            if (autoUpdateTransformData != prototype.autoUpdateTransformData && prototype.meshRenderersDisabled)
                SetRenderersEnabled(prototype, !prototype.meshRenderersDisabled);
            EditorGUI.EndDisabledGroup();

            if (!prototype.enableRuntimeModifications)
            {
                if (prototype.addRemoveInstancesAtRuntime)
                    prototype.addRemoveInstancesAtRuntime = false;
                if (prototype.startWithRigidBody)
                    prototype.startWithRigidBody = false;
                if (prototype.autoUpdateTransformData)
                {
                    prototype.autoUpdateTransformData = false;
                    if (prototype.meshRenderersDisabled)
                        SetRenderersEnabled(prototype, !prototype.meshRenderersDisabled);
                }
            }

            if ((!prototype.enableRuntimeModifications || !prototype.addRemoveInstancesAtRuntime) && prototype.extraBufferSize > 0)
                prototype.extraBufferSize = 0;

            if ((!prototype.enableRuntimeModifications || !prototype.addRemoveInstancesAtRuntime) && prototype.addRuntimeHandlerScript)
            {
                prototype.addRuntimeHandlerScript = false;
                GPUInstancerPrefabRuntimeHandler prefabRuntimeHandler = prototype.prefabObject.GetComponent<GPUInstancerPrefabRuntimeHandler>();
                if (prefabRuntimeHandler != null)
                {
#if UNITY_2018_3_OR_NEWER
                        GPUInstancerUtility.RemoveComponentFromPrefab<GPUInstancerPrefabRuntimeHandler>(prototype.prefabObject);
#else
                    DestroyImmediate(prefabRuntimeHandler, true);
#endif
                    EditorUtility.SetDirty(prototype.prefabObject);
                }
            }

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeActions()
        {
            if (Application.isPlaying)
                return;

            GUILayout.Space(10);

            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_actions, GPUInstancerEditorConstants.Styles.boldLabel, false);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.delete, Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deleteAreYouSure + "\n\"" + _prefabManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_remove, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deletePrototypeAreYouSure + "\n\"" + _prefabManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_delete, GPUInstancerEditorConstants.TEXT_keepPrototypeDefinition))
                        {
                            if (((GPUInstancerPrefabPrototype)_prefabManager.selectedPrototype).meshRenderersDisabled)
                                SetRenderersEnabled((GPUInstancerPrefabPrototype)_prefabManager.selectedPrototype, true);
                            _prefabManager.DeletePrototype(_prefabManager.selectedPrototype);
                            _prefabManager.selectedPrototype = null;
                        }
                        else
                        {
                            _prefabManager.DeletePrototype(_prefabManager.selectedPrototype, false);
                            _prefabManager.selectedPrototype = null;
                        }
                    }
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_delete);
        }

        public override void DrawGPUInstancerPrototypeAdvancedActions()
        {
            if (Application.isPlaying)
                return;

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            // title
            Rect foldoutRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            foldoutRect.x += 12;
            showAdvancedBox = EditorGUI.Foldout(foldoutRect, showAdvancedBox, GPUInstancerEditorConstants.TEXT_advancedActions, true, GPUInstancerEditorConstants.Styles.foldout);

            //GUILayout.Space(10);

            if (showAdvancedBox)
            {
                EditorGUILayout.HelpBox(GPUInstancerEditorConstants.HELPTEXT_advancedActions, MessageType.Warning);

                GPUInstancerPrefabPrototype prefabPrototype = (GPUInstancerPrefabPrototype)_prefabManager.selectedPrototype;

                if (prefabPrototype != null)
                {
                    if (prefabPrototype.meshRenderersDisabled)
                    {
                        GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.enableMeshRenderers, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                            () =>
                            {
                                GPUInstancerPrefabManagerEditor.SetRenderersEnabled(prefabPrototype, true);
                            });
                    }
                    else
                    {
                        GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.disableMeshRenderers, GPUInstancerEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, Rect.zero,
                        () =>
                        {
                            if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_disableMeshRenderers, GPUInstancerEditorConstants.TEXT_disableMeshRenderersAreYouSure, "Yes", "No"))
                            {
                                GPUInstancerPrefabManagerEditor.SetRenderersEnabled(prefabPrototype, false);
                            }
                        });
                    }
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_disableMeshRenderers);
                }
            }

            EditorGUILayout.EndVertical();
        }

        public override float GetMaxDistance(GPUInstancerPrototype selectedPrototype)
        {
            return GPUInstancerEditorConstants.MAX_PREFAB_DISTANCE;
        }
    }
}