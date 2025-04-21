using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime;

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
    }
}