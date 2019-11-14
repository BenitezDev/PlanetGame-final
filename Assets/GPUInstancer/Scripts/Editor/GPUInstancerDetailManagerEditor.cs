using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    [CustomEditor(typeof(GPUInstancerDetailManager))]
    [CanEditMultipleObjects]
    public class GPUInstancerDetailManagerEditor : GPUInstancerManagerEditor
    {
        protected SerializedProperty prop_runInThreads;

        private GPUInstancerDetailManager _detailManager;

        protected override void OnEnable()
        {
            base.OnEnable();

            wikiHash = "#The_Detail_Manager";

            prop_runInThreads = serializedObject.FindProperty("runInThreads");

            _detailManager = (target as GPUInstancerDetailManager);
            if (!Application.isPlaying && _detailManager.gpuiSimulator == null)
                _detailManager.gpuiSimulator = new GPUInstancerEditorSimulator(_detailManager);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!Application.isPlaying && _detailManager.gpuiSimulator != null && _detailManager.gpuiSimulator.simulateAtEditor)
                _detailManager.gpuiSimulator.StopSimulation();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            if (_detailManager.terrain == null)
            {
                if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _detailManager.pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
                {
                    if (EditorGUIUtility.GetObjectPickerControlID() == _detailManager.pickerControlID)
                        _detailManager.AddTerrainPickerObject(EditorGUIUtility.GetObjectPickerObject());
                    _detailManager.pickerControlID = -1;
                }

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                DrawDetailTerrainAddButton();
                EditorGUI.EndDisabledGroup();
                return;
            }
            else if (_detailManager.terrainSettings == null)
                _detailManager.SetupManagerWithTerrain(_detailManager.terrain);

            DrawSceneSettingsBox();

            if (_detailManager.terrainSettings != null)
            {
                DrawDebugBox(_detailManager.gpuiSimulator);

                DrawDetailGlobalInfoBox();

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
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_terrain, _detailManager.terrain, typeof(Terrain), true);
            EditorGUI.EndDisabledGroup();


            EditorGUILayout.BeginHorizontal();
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.paintOnTerrain, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (_detailManager.terrain != null)
                    {
                        GPUInstancerTerrainProxy proxy = _detailManager.AddProxyToTerrain();
                        Selection.activeGameObject = _detailManager.terrain.gameObject;

                        proxy.terrainSelectedToolIndex = 5;
                    }
                });
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.removeTerrain, Color.red, Color.white, FontStyle.Bold, Rect.zero,
            () =>
            {
                if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_removeTerrainConfirmation, GPUInstancerEditorConstants.TEXT_removeTerrainAreYouSure, GPUInstancerEditorConstants.TEXT_unset, GPUInstancerEditorConstants.TEXT_cancel))
                {
                    _detailManager.SetupManagerWithTerrain(null);
                }
            });
            EditorGUILayout.EndHorizontal();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_terrain);

            EditorGUILayout.Space();

            EditorGUI.EndDisabledGroup();

            DrawCameraDataFields();

            DrawCullingSettings(_detailManager.prototypeList);
        }

        public void DrawDetailTerrainAddButton()
        {
            GUILayout.Space(10);
            Rect buttonRect = GUILayoutUtility.GetRect(100, 40, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.setTerrain, GPUInstancerEditorConstants.Colors.lightBlue, Color.black, FontStyle.Bold, buttonRect,
                () =>
                {
                    _detailManager.pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    _detailManager.ShowTerrainPicker();
                },
                true, true,
                (o) =>
                {
                    _detailManager.AddTerrainPickerObject(o);
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_setTerrain, true);
            GUILayout.Space(10);
        }

        public void DrawDetailGlobalInfoBox()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_detailGlobal, GPUInstancerEditorConstants.Styles.boldLabel);
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_terrainSettingsSO, _detailManager.terrainSettings, typeof(GPUInstancerTerrainSettings), false);
            EditorGUI.EndDisabledGroup();

            float newMaxDetailDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_maxDetailDistance, _detailManager.terrainSettings.maxDetailDistance, 0, GPUInstancerEditorConstants.MAX_DETAIL_DISTANCE);
            if (_detailManager.terrainSettings.maxDetailDistance != newMaxDetailDistance)
            {
                foreach (GPUInstancerDetailPrototype p in _detailManager.prototypeList)
                {
                    if (p.maxDistance == _detailManager.terrainSettings.maxDetailDistance || p.maxDistance > newMaxDetailDistance)
                    {
                        p.maxDistance = newMaxDetailDistance;
                        EditorUtility.SetDirty(p);
                    }
                }
                _detailManager.terrainSettings.maxDetailDistance = newMaxDetailDistance;
            }
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_maxDetailDistance);
            EditorGUILayout.Space();

            float newDetailDensity = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_detailDensity, _detailManager.terrainSettings.detailDensity, 0.0f, 1.0f);
            if (_detailManager.terrainSettings.detailDensity != newDetailDensity)
            {
                foreach (GPUInstancerDetailPrototype p in _detailManager.prototypeList)
                {
                    if (p.detailDensity == _detailManager.terrainSettings.detailDensity || p.detailDensity > newDetailDensity)
                    {
                        p.detailDensity = newDetailDensity;
                        EditorUtility.SetDirty(p);
                    }
                }
                _detailManager.terrainSettings.detailDensity = newDetailDensity;
            }
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailDensity);
            EditorGUILayout.Space();

            _detailManager.detailLayer = EditorGUILayout.LayerField(GPUInstancerEditorConstants.TEXT_detailLayer, _detailManager.detailLayer);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailLayer);
            EditorGUILayout.Space();

            _detailManager.terrainSettings.windVector = EditorGUILayout.Vector2Field(GPUInstancerEditorConstants.TEXT_windVector, _detailManager.terrainSettings.windVector);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windVector);
            EditorGUILayout.Space();

            _detailManager.terrainSettings.healthyDryNoiseTexture = (Texture2D)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_healthyDryNoiseTexture, _detailManager.terrainSettings.healthyDryNoiseTexture, typeof(Texture2D), false);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_healthyDryNoiseTexture);
            if (_detailManager.terrainSettings.healthyDryNoiseTexture == null)
                _detailManager.terrainSettings.healthyDryNoiseTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_HEALTHY_DRY_NOISE);

            _detailManager.terrainSettings.windWaveNormalTexture = (Texture2D)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_windWaveNormalTexture, _detailManager.terrainSettings.windWaveNormalTexture, typeof(Texture2D), false);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveNormalTexture);
            if (_detailManager.terrainSettings.windWaveNormalTexture == null)
                _detailManager.terrainSettings.windWaveNormalTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_WIND_WAVE_NOISE);

            _detailManager.terrainSettings.autoSPCellSize = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_autoSPCellSize, _detailManager.terrainSettings.autoSPCellSize);
            if (!_detailManager.terrainSettings.autoSPCellSize)
                _detailManager.terrainSettings.preferedSPCellSize = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_preferedSPCellSize, _detailManager.terrainSettings.preferedSPCellSize, 25, 500);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_spatialPartitioningCellSize);

            EditorGUILayout.PropertyField(prop_runInThreads, GPUInstancerEditorConstants.Contents.runInThreads);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_runInThreads);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_detailManager, "Editor data changed.");
                _detailManager.OnEditorDataChanged();
                EditorUtility.SetDirty(_detailManager.terrainSettings);
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

            if (!string.IsNullOrEmpty(_detailManager.terrainSettings.warningText))
                EditorGUILayout.HelpBox(_detailManager.terrainSettings.warningText, MessageType.Error);

            if (!Application.isPlaying)
            {
                GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.generatePrototypes, GPUInstancerEditorConstants.Colors.darkBlue, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_generatePrototypesConfirmation, GPUInstancerEditorConstants.TEXT_generatePrototypeAreYouSure, GPUInstancerEditorConstants.TEXT_generatePrototypes, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _detailManager.GeneratePrototypes(true);
                    }
                });
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_generatePrototypesDetail);
            }

            if (prototypeContents == null || prototypeContents.Length != _detailManager.prototypeList.Count)
                GeneratePrototypeContents();

            int i = 0;
            EditorGUILayout.BeginHorizontal();
            foreach (GPUInstancerPrototype prototype in _detailManager.prototypeList)
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
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_addprototypedetail);

            DrawGPUInstancerPrototypeBox(_detailManager.selectedPrototype, _detailManager.isFrustumCulling, _detailManager.isOcclusionCulling,
               _detailManager.shaderBindings, _detailManager.billboardAtlasBindings);

            EditorGUILayout.EndVertical();
        }

        public override void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype)
        {
            DrawGPUInstancerPrototypeInfo(selectedPrototype, (string t) => { DrawHelpText(t); }, _detailManager, _detailManager.OnEditorDataChanged, _detailManager.shaderBindings,
                _detailManager.gpuiSimulator, _detailManager.terrainSettings, _detailManager.detailLayer);
        }

        public static void DrawGPUInstancerPrototypeInfo(GPUInstancerPrototype selectedPrototype, UnityAction<string> DrawHelpText, UnityEngine.Object component, UnityAction OnEditorDataChanged,
            GPUInstancerShaderBindings shaderBindings, GPUInstancerEditorSimulator simulator, GPUInstancerTerrainSettings terrainSettings, int detailLayer)
        {
            GPUInstancerDetailPrototype prototype = (GPUInstancerDetailPrototype)selectedPrototype;

            EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_detailProperties, GPUInstancerEditorConstants.Styles.boldLabel);

            EditorGUI.BeginChangeCheck();

            prototype.detailDensity = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_detailDensity, prototype.detailDensity, 0.0f, terrainSettings.detailDensity);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailDensity);
            prototype.detailScale = EditorGUILayout.Vector4Field(GPUInstancerEditorConstants.TEXT_detailScale, prototype.detailScale);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailScale);

            prototype.noiseSpread = EditorGUILayout.FloatField(GPUInstancerEditorConstants.TEXT_noiseSpread, prototype.noiseSpread);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_noiseSpread);

            prototype.useCustomHealthyDryNoiseTexture = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useCustomHealthyDryNoiseTexture, prototype.useCustomHealthyDryNoiseTexture);
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useCustomHealthyDryNoiseTexture);
            if (prototype.useCustomHealthyDryNoiseTexture)
            {
                prototype.healthyDryNoiseTexture = (Texture2D)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_healthyDryNoiseTexture, prototype.healthyDryNoiseTexture, typeof(Texture2D), false);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_healthyDryNoiseTexture);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(component, "Editor data changed.");
                if (OnEditorDataChanged != null)
                    OnEditorDataChanged();
                EditorUtility.SetDirty(prototype);
            }

            EditorGUI.BeginChangeCheck();
            if (!prototype.usePrototypeMesh)
            {
                prototype.useCustomMaterialForTextureDetail = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_useCustomMaterialForTextureDetail, prototype.useCustomMaterialForTextureDetail);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_useCustomMaterialForTextureDetail);
                if (prototype.useCustomMaterialForTextureDetail)
                {
                    prototype.textureDetailCustomMaterial = (Material)EditorGUILayout.ObjectField(GPUInstancerEditorConstants.TEXT_textureDetailCustomMaterial, prototype.textureDetailCustomMaterial, typeof(Material), false);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_textureDetailCustomMaterial);
                    prototype.isBillboard = false;
                }
                else
                {
                    prototype.textureDetailCustomMaterial = null;
                }
            }

            EditorGUILayout.EndVertical();

            if (!prototype.usePrototypeMesh && !prototype.isBillboard)
            {
                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_crossQuads, GPUInstancerEditorConstants.Styles.boldLabel);

                prototype.useCrossQuads = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_crossQuads, prototype.useCrossQuads);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_crossQuads);

                if (prototype.useCrossQuads)
                {
                    prototype.quadCount = EditorGUILayout.IntSlider(GPUInstancerEditorConstants.TEXT_quadCount, prototype.quadCount, 2, 4);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_quadCount);

                    if (!prototype.useCustomMaterialForTextureDetail)
                    {
                        prototype.billboardDistance = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_billboardDistance, prototype.billboardDistance, 0.5f, 1f);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistance);
                        prototype.billboardDistanceDebug = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_billboardDistanceDebug, prototype.billboardDistanceDebug);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistanceDebug);
                        if (prototype.billboardDistanceDebug)
                        {
                            prototype.billboardDistanceDebugColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_billboardDistanceDebugColor, prototype.billboardDistanceDebugColor);
                            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardDistanceDebugColor);
                        }
                        prototype.billboardFaceCamPos = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_CQBillboardFaceCamPos, prototype.billboardFaceCamPos);
                        DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_CQBillboardFaceCamPos);
                    }

                }
                else
                {
                    prototype.quadCount = 1;
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                prototype.useCrossQuads = false;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (!prototype.usePrototypeMesh && prototype.useCustomMaterialForTextureDetail && prototype.textureDetailCustomMaterial != null)
                {
                    if (!shaderBindings.IsShadersInstancedVersionExists(prototype.textureDetailCustomMaterial.shader.name))
                    {
                        Shader instancedShader;
                        if (GPUInstancerUtility.IsShaderInstanced(prototype.textureDetailCustomMaterial.shader))
                            instancedShader = prototype.textureDetailCustomMaterial.shader;
                        else
                            instancedShader = GPUInstancerUtility.CreateInstancedShader(prototype.textureDetailCustomMaterial.shader, shaderBindings);

                        if (instancedShader != null)
                            shaderBindings.AddShaderInstance(prototype.textureDetailCustomMaterial.shader.name, instancedShader);
                        else
                            Debug.LogWarning("Can not create instanced version for shader: " + prototype.textureDetailCustomMaterial.shader.name + ". Standard Shader will be used instead.");
                    }
                }
                EditorUtility.SetDirty(prototype);
            }

            if (!prototype.usePrototypeMesh && !prototype.useCustomMaterialForTextureDetail)
            {
                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_foliageShaderProperties, GPUInstancerEditorConstants.Styles.boldLabel);

                EditorGUI.BeginChangeCheck();
                prototype.isBillboard = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_isBillboard, prototype.isBillboard);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_isBillboard);

                if (prototype.isBillboard)
                {
                    prototype.billboardFaceCamPos = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_billboardFaceCamPos, prototype.billboardFaceCamPos);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_billboardFaceCamPos);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(prototype);
                    if (simulator != null && simulator.simulateAtEditor && !simulator.initializingInstances)
                    {
                        GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(simulator.gpuiManager.runtimeDataList, terrainSettings, false, detailLayer);
                    }
                }

                EditorGUI.BeginChangeCheck();

                prototype.detailHealthyColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_detailHealthyColor, prototype.detailHealthyColor);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailHealthyColor);
                prototype.detailDryColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_detailDryColor, prototype.detailDryColor);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_detailDryColor);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(component, "Editor data changed.");
                    if (OnEditorDataChanged != null)
                        OnEditorDataChanged();
                    if (simulator != null && simulator.simulateAtEditor && !simulator.initializingInstances)
                    {
                        GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(simulator.gpuiManager.runtimeDataList, terrainSettings, false, detailLayer);
                    }
                    EditorUtility.SetDirty(prototype);
                }

                EditorGUI.BeginChangeCheck();

                prototype.ambientOcclusion = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_ambientOcclusion, prototype.ambientOcclusion, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_ambientOcclusion);
                prototype.gradientPower = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_gradientPower, prototype.gradientPower, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_gradientPower);

                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_windSettings, GPUInstancerEditorConstants.Styles.boldLabel);

                prototype.windIdleSway = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windIdleSway, prototype.windIdleSway, 0f, 1f);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windIdleSway);
                prototype.windWavesOn = EditorGUILayout.Toggle(GPUInstancerEditorConstants.TEXT_windWavesOn, prototype.windWavesOn);
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWavesOn);
                if (prototype.windWavesOn)
                {
                    prototype.windWaveTintColor = EditorGUILayout.ColorField(GPUInstancerEditorConstants.TEXT_windWaveTintColor, prototype.windWaveTintColor);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveTintColor);
                    prototype.windWaveSize = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveSize, prototype.windWaveSize, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveSize);
                    prototype.windWaveTint = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveTint, prototype.windWaveTint, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveTint);
                    prototype.windWaveSway = EditorGUILayout.Slider(GPUInstancerEditorConstants.TEXT_windWaveSway, prototype.windWaveSway, 0f, 1f);
                    DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_windWaveSway);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(prototype);
                    if (simulator != null && simulator.simulateAtEditor && !simulator.initializingInstances)
                    {
                        GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(simulator.gpuiManager.runtimeDataList, terrainSettings, false, detailLayer);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        public override void DrawGPUInstancerPrototypeActions()
        {
            GUILayout.Space(10);
            GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_actions, GPUInstancerEditorConstants.Styles.boldLabel, false);

            if (!_detailManager.editorDataChanged)
                EditorGUI.BeginDisabledGroup(true);
            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.applyChangesToTerrain, GPUInstancerEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    _detailManager.ApplyEditorDataChanges();
                });
            if (!_detailManager.editorDataChanged)
                EditorGUI.EndDisabledGroup();
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_applyChangesToTerrain);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.delete, Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    if (EditorUtility.DisplayDialog(GPUInstancerEditorConstants.TEXT_deleteConfirmation, GPUInstancerEditorConstants.TEXT_deleteAreYouSure + "\n\"" + _detailManager.selectedPrototype.ToString() + "\"", GPUInstancerEditorConstants.TEXT_delete, GPUInstancerEditorConstants.TEXT_cancel))
                    {
                        _detailManager.DeletePrototype(_detailManager.selectedPrototype);
                        _detailManager.selectedPrototype = null;
                    }
                });
            DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_delete);
        }

        public override float GetMaxDistance(GPUInstancerPrototype selectedPrototype)
        {
            return _detailManager.terrainSettings != null ? _detailManager.terrainSettings.maxDetailDistance : GPUInstancerEditorConstants.MAX_DETAIL_DISTANCE;
        }
    }
}