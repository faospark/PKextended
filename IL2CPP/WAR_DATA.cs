using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    [System.Serializable]
    public class WAR_DATA : global::Il2CppSystem.Object
    {
        private static readonly System.IntPtr NativeFieldInfoPtr_leader_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_sub_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_sub_to_leader;

        static WAR_DATA()
        {
            Il2CppClassPointerStore<WAR_DATA>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "WAR_DATA");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WAR_DATA>.NativeClassPtr);
            
            NativeFieldInfoPtr_leader_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_DATA>.NativeClassPtr, "leader_no");
            NativeFieldInfoPtr_sub_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_DATA>.NativeClassPtr, "sub_no");
            NativeFieldInfoPtr_sub_to_leader = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_DATA>.NativeClassPtr, "sub_to_leader");
        }

        public WAR_DATA(System.IntPtr pointer) : base(pointer) { }

        // Array of leader character indices
        public unsafe Il2CppStructArray<byte> leader_no
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_leader_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(arrayPtr) : null;
            }
        }

        // Array of sub-character indices
        public unsafe Il2CppStructArray<byte> sub_no
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_sub_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(arrayPtr) : null;
            }
        }

        // Mapping of sub characters to their leader
        public unsafe Il2CppStructArray<sbyte> sub_to_leader
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_sub_to_leader);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<sbyte>(arrayPtr) : null;
            }
        }
    }
}
