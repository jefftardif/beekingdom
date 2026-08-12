using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BeeKingdom.Networking
{
    internal static class IosKeychainBridge
    {
        private const string ServiceName = "com.BKD-Honey-Studio.BeeKingdom.keychain";

        public static bool IsAvailable
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return Application.platform == RuntimePlatform.IPhonePlayer;
#else
                return false;
#endif
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int BeeKingdomKeychain_Set(string service, string account, string value);

        [DllImport("__Internal")]
        private static extern IntPtr BeeKingdomKeychain_Get(string service, string account);

        [DllImport("__Internal")]
        private static extern int BeeKingdomKeychain_Delete(string service, string account);

        [DllImport("__Internal")]
        private static extern void BeeKingdomKeychain_FreeString(IntPtr value);

        public static void Set(string account, string value)
        {
            int status = BeeKingdomKeychain_Set(ServiceName, account, value);
            if (status != 0) throw new InvalidOperationException("iOS Keychain write failed with OSStatus " + status.ToString());
        }

        public static string TryGet(string account)
        {
            IntPtr pointer = BeeKingdomKeychain_Get(ServiceName, account);
            if (pointer == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringUTF8(pointer);
            }
            finally
            {
                BeeKingdomKeychain_FreeString(pointer);
            }
        }

        public static void Delete(string account)
        {
            int status = BeeKingdomKeychain_Delete(ServiceName, account);
            if (status != 0) throw new InvalidOperationException("iOS Keychain delete failed with OSStatus " + status.ToString());
        }
#else
        public static void Set(string account, string value)
        {
            throw new InvalidOperationException("iOS Keychain is unavailable on this platform.");
        }

        public static string TryGet(string account)
        {
            return null;
        }

        public static void Delete(string account)
        {
        }
#endif
    }
}
