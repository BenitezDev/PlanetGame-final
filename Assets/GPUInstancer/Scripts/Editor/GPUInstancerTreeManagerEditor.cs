using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerTreeManager))]
    [CanEditMultipleObjects]
    public class GPUInstancerTreeManagerEditor : GPUInstancerManagerEditor
    {
        private GPUInstancerTreeManager _treeManager;

        protected override void OnEnable()
        {
            base.OnEnable();

            wikiHash = "#The_Tree_Manager";

            _treeManager = (target as GPUInstancerTreeManager);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (_treeManager.terrain == null)
            {
                if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _treeManager.pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
                {
                    if (EditorGUIUtility.GetObjectPickerControlID() == _treeManager.pickerControlID)
                        _treeManager.AddTerrainPickerObject(EditorGUIUtility.GetObjectPickerObject());
                    _treeManager.pickerControlID = -1;
                }

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                DrawTreeTerrainAddButton();
                EditorGUI.EndDisabledGroup();
                return;
            }
            else if (_treeManager.terrainSettings == null)
                _treeManager.SetupManagerWithTerrain(_treeManager.terrain);

            DrawSceneSettingsBox();

            if (_treeManager.terrainSettings != null)
            {
                DrawTreeGlobalInfoBox();

                DrawGPUInstancerManagerGUILayout();
            }

            HandlePickerObjectSelection();

            serializedObject.ApplyModifiedProperties();

            base.InspectorGUIEnd();
        }

        public override void DrawSettingContents()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            //EditorGUILayout.PropertyField(prop_settings);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_terrain, _treeManager.terrain, typeof(Terrain), true);
            EditorGUI.EndDisabledGroup();


            EditorGUILayout.BeginHorizontal();
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.paintOnTerrain, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (_treeManager.terrain != null)
                    {
                        GPUInstancerTerrainProxy proxy = _treeManager.AddProxyToTerrain();
                        Selection.activeGameObject = _treeManager.terrain.gameObject;

                        proxy.terrainSelectedToolIndex = 4;
                    }
                });
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.removeTerrain, Color.red, Color.white, FontStyle.Bold, Rect.zero,
            () =>
            {
                if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_removeTerrainConfirmation, GPUInstancerEditorConstants.TEXT_removeTerrainAreYouSure, GPUInstancerEditorConstants.TEXT_unset, GPUInstancerEditorConstants.TEXT_cancel))
                {
                    _treeManager.SetupManagerWithTerrain(null);
                }
            });
            EditorGUILayout.EndHorizontal();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_terrain);

            EditorGUILayout.Space();

            EditorGUI.EndDisabledGroup();

            DrawCameraDataFields();

            DrawCullingSettings(_treeManager.prototypeList);
        }

        public void DrawTreeTerrainAddButton()
        {
            GUILayout.Space(10);
            Rect buttonRect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.setTerrain, GPUInstancerEditorConstants.Colors.lightBlue, Color.black, FontStyle.Bold, buttonRect,
                () =>
                {
                    _treeManager.pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    _treeManager.ShowTerrainPicker();
                },
                true, true,
                (o) =>
                {
                    _treeManager.AddTerrainPickerObject(o);
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_setTerrain, true);
            GUILayout.Space(10);
        }

        public void DrawTreeGlobalInfoBox()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_treeGlobal, GPUInstancerEditorConstants.Styles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_terrainSettingsSO, _treeManager.terrainSettings, typeof(GPUInstancerTerrainSettings), false);
            EditorGUI.EndDisabledGroup();

            float newMaxTreeDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_maxTreeDistance, _treeManager.terrainSettings.maxTreeDistance, 0, GPUInstancerEditorConstants.MAX_TREE_DISTANCE);
            if (_treeManager.terrainSettings.maxTreeDistance != newMaxTreeDistance)
            {
                foreach (GPUInstancerPrototype p in _treeManager.prototypeList)
                {
                    if (p.maxDistance == _treeManager.terrainSettings.maxTreeDistance || p.maxDistance > newMaxTreeDistance)
                    {
                        p.maxDistance = newMaxTreeDistance;
                        EditorUtility.SetDirty(p);
                    }
                }
                _treeManager.terrainSettings.maxTreeDistance = newMaxTreeDistance;
            }
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_maxTreeDistance);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_treeManager, "Editor data changed.");
                _treeManager.OnEditorDataChanged();
                EditorUtility.SetDirty(_treeManager.terrainSettings);
            }

            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        public void DrawGPUInstancerManagerGUILayout()
        {

            int prototypeRowCount = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30f) / PROTOTYPE_RECT_SIZE);

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_prototypes, GPUInstancerEditorConstants.Styles.boldLabel);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_prototypes);

            if (!Application.isPlaying)
            {
                GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.generatePrototypes, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_generatePrototypesConfirmation, GPUInstancerEditorConstants.TEXT_generatePrototypeAreYouSure, GPUInstancerEditorConstants.TEXT_generatePrototypes, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _treeManager.GeneratePrototypes(true);
                    }
                });
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_generatePrototypesTree);

                GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.regenerateBillboards, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_regenerateBillboardsConfirmation, GPUInstancerEditorConstants.TEXT_regenerateBillboardsAreYouSure, GPUInstancerEditorConstants.TEXT_regenerateBillboards, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        foreach (GPUInstancerPrototype prototype in _treeManager.prototypeList)
                        {
                            if (prototype.useGeneratedBillboard)
                            {
                                GPUInstancerUtility.GeneratePrototypeBillboard(prototype, _treeManager.billboardAtlasBindings, true);
                            }
                        }
                    }
                });
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_regenerateBillboards);
            }

            if (prototypeContents == null || prototypeContents.Length != _treeManager.prototypeList.Count)
                GeneratePrototypeContents();

            int i = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (GPUInstancerPrototype prototype in _treeManager.prototypeList)
            {
                if (prototype == null)
                    continue;
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
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addprototypetree);

            DrawGPUInstancerPrototypeBox(_treeManager.selectedPrototype, prop_isManagerFrustumCulling.boolValue, prop_isManagerOcclusionCulling.boolValue, 
                _treeManager.shaderBindings, _treeManager.billboardAtlasBindings);

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype)
        {

            DrawGPUInstancerPrototypeInfo(selectedPrototype, (string t) => { DrawHelpText(t); }, _treeManager, null, _treeManager.shaderBindings,
                    null, _treeManager.terrainSettings);
        }

        public static void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype, UnityAction<string> DrawHelpText, Object component, UnityAction OnEditorDataChanged,
            GPUInstancerShaderBindings shaderBindings, GPUInstancerEditorSimulator simulator, GPUInstancerTerrainSettings terrainSettings)
        {
            GPUInstancerTreePrototype treePrototype = (GPUInstancerTreePrototype)selectedPrototype;
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_treeSettings, GPUInstancerEditorConstants.Styles.boldLabel);

            treePrototype.isApplyRotation = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useRandomTreeTotation, treePrototype.isApplyRotation);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useRandomTreeTotation);

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeActions()
        {
            GUILayout.Space(10);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_actions, GPUInstancerEditorConstants.Styles.boldLabel, false);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.delete, Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deleteAreYouSure + "\n\"" + _treeManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_delete, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _treeManager.DeletePrototype(_treeManager.selectedPrototype);
                        _treeManager.selectedPrototype = null;
                    }
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_delete);
        }

        public override float GetMaxDistance(GPUInstancerPrototype selectedPrototype)
        {
            return _treeManager.terrainSettings != null ? _treeManager.terrainSettings.maxTreeDistance : GPUInstancerEditorConstants.MAX_TREE_DISTANCE;
        }
    }
}