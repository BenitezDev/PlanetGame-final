using UnityEditor;
using UnityEngine;

namespace GPUInstancer
{
    public abstract class GPUInstancerManagerEditor : GPUInstancerEditor
    {
        private GPUInstancerManager _manager;

#if !UNITY_2017_1_OR_NEWER
        private PreviewRenderUtility _previewRenderUtility;
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            prototypeContents = null;

            _manager = (target as GPUInstancerManager);
            FillPrototypeList();
        }

        public override void FillPrototypeList()
        {
            prototypeList = _manager.prototypeList;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying && _manager.cameraData != null && _manager.cameraData.mainCamera == null)
                EditorGUILayout.HelpBox(GPUInstancerEditorConstants.ERRORTEXT_cameraNotFound, MessageType.Error);
        }

        public bool HandlePickerObjectSelection()
        {
            if (!Application.isPlaying && Event.current.type == EventType.ExecuteCommand && _manager.pickerControlID > 0 && Event.current.commandName == "ObjectSelectorClosed")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == _manager.pickerControlID)
                    _manager.AddPickerObject(EditorGUIUtility.GetObjectPickerObject());
                _manager.pickerControlID = -1;
                return true;
            }
            return false;
        }

        public void DrawDebugBox(GPUInstancerEditorSimulator simulator = null)
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(GPUInstancerEditorConstants.Styles.box);
                GPUInstancerEditorConstants.DrawCustomLabel(GPUInstancerEditorConstants.TEXT_debug, GPUInstancerEditorConstants.Styles.boldLabel);

                if (simulator != null)
                {
                    if (simulator.simulateAtEditor)
                    {
                        if (simulator.initializingInstances)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditorPrep, GPUInstancerEditorConstants.Colors.darkBlue, Color.white,
                                FontStyle.Bold, Rect.zero, null);
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditorStop, Color.red, Color.white,
                                FontStyle.Bold, Rect.zero, () =>
                                {
                                    simulator.StopSimulation();
                                });
                        }
                    }
                    else
                    {
                        GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.simulateAtEditor, GPUInstancerEditorConstants.Colors.green, Color.white,
                            FontStyle.Bold, Rect.zero, () =>
                            {
                                simulator.StartSimulation();
                            });
                    }
                }
                DrawHelpText(GPUInstancerEditorConstants.HELPTEXT_simulator);

                EditorGUILayout.EndVertical();
            }
        }

        public void DrawGPUInstancerPrototypeButton(GPUInstancerPrototype prototype, GUIContent prototypeContent)
        {
            base.DrawGPUInstancerPrototypeButton(prototype, prototypeContent, _manager.selectedPrototype == prototype, () =>
            {
                _manager.selectedPrototype = prototype;
                GUI.FocusControl(prototypeContent.tooltip);
            });
        }

        public void DrawGPUInstancerPrototypeAddButton()
        {
            Rect prototypeRect = GUILayoutUtility.GetRect(PROTOTYPE_RECT_SIZE, PROTOTYPE_RECT_SIZE, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));

            Rect iconRect = new Rect(prototypeRect.position + PROTOTYPE_RECT_PADDING_VECTOR, PROTOTYPE_RECT_SIZE_VECTOR);

            GPUInstancerEditorConstants.DrawColoredButton(GPUInstancerEditorConstants.Contents.add, GPUInstancerEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, iconRect,
                () =>
                {
                    _manager.pickerControlID = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                    _manager.ShowObjectPicker();
                },
                true, true,
                (o) =>
                {
                    _manager.AddPickerObject(o);
                });
        }
    }
}
