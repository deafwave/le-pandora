using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime;
using Il2Cpp;
using MelonLoader;

namespace LastEpochPandora.Services
{
    public static class UtilityService
    {
        public static bool IsNullOrDestroyed(this object obj)
        {
            if (obj == null) return true;
            if (obj is UnityEngine.Object unityObj)
                return !unityObj;

            return false;
        }
        public static bool IsValid(this Il2CppObjectBase obj)
        {
            try
            {
                if (obj == null || obj.IsNullOrDestroyed())
                    return false;
                return obj.Pointer != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        public static void LogAllComponents(GameObject obj, string prefix = "")
        {
            if (obj == null)
            {
                MelonLogger.Msg($"{prefix}Object is null");
                return;
            }

            MelonLogger.Msg($"{prefix}Components on {obj.name}:");

            Component[] components = obj.GetComponents<Component>();
            foreach (Component component in components)
            {
                MelonLogger.Msg($"{prefix}  - {component.GetType().Name}");

                if (component is MonoBehaviour)
                {
                    var fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public |
                                                              System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        try
                        {
                            var value = field.GetValue(component);
                            MelonLogger.Msg($"{prefix}    {field.Name} = {(value != null ? value.ToString() : "null")}");
                        }
                        catch (Exception) {  }
                    }
                }
            }
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform child = obj.transform.GetChild(i);
                MelonLogger.Msg($"{prefix}Child: {child.name}");
                LogAllComponents(child.gameObject, prefix + "  ");
            }
        }
    }
}