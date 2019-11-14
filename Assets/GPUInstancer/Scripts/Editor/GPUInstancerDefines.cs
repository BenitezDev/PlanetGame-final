using GPUInstancer.Extention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace GPUInstancer
{
    [InitializeOnLoad]
    public class GPUInstancerDefines
    {
        private static readonly string DEFINE_GPU_INSTANCER = "GPU_INSTANCER";

        // billboard extentions
        private static Type _billboardExtentionType;
        private static Assembly _billboardExtentionAssebly;
        public static List<GPUInstancerBillboardExtention> billboardExtentions;

        static GPUInstancerDefines()
        {
            List<string> defineList = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Split(';'));
            if (!defineList.Contains(DEFINE_GPU_INSTANCER))
            {
                defineList.Add(DEFINE_GPU_INSTANCER);
                string defines = string.Join(";", defineList.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }

            GetBillboardExtentions();
        }

        static void GetBillboardExtentions()
        {
            try
            {
                if (billboardExtentions == null)
                    billboardExtentions = new List<GPUInstancerBillboardExtention>();

                if (_billboardExtentionType == null)
                    _billboardExtentionType = typeof(GPUInstancerBillboardExtention);

                if (_billboardExtentionAssebly == null)
                    _billboardExtentionAssebly = Assembly.GetAssembly(_billboardExtentionType);

                IEnumerable<Type> types = _billboardExtentionAssebly.GetTypes()
                    .Where(p => _billboardExtentionType.IsAssignableFrom(p) && p != _billboardExtentionType);

                foreach (Type type in types)
                {
                    try
                    {
                        ConstructorInfo ci = type.GetConstructor(new Type[] { });
                        GPUInstancerBillboardExtention billboardExtention = (GPUInstancerBillboardExtention)ci.Invoke(new object[] { });
                        billboardExtentions.Add(billboardExtention);
                    }
                    catch (Exception) { }
                }

            }
            catch (Exception) { }
        }
    }
}
