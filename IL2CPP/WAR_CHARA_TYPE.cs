using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    public class WAR_CHARA_TYPE : global::Il2CppSystem.Object
    {
        private static readonly System.IntPtr NativeFieldInfoPtr_nouryoku;
        private static readonly System.IntPtr NativeFieldInfoPtr_kaisu;
        private static readonly System.IntPtr NativeFieldInfoPtr_kaisu_max;
        private static readonly System.IntPtr NativeFieldInfoPtr_face_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_name;
        private static readonly System.IntPtr NativeFieldInfoPtr_attack;
        private static readonly System.IntPtr NativeFieldInfoPtr_defense;


        static WAR_CHARA_TYPE()
        {
            Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "WAR_CHARA_TYPE");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr);
            
            NativeFieldInfoPtr_nouryoku = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "nouryoku");
            NativeFieldInfoPtr_kaisu = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "kaisu");
            NativeFieldInfoPtr_kaisu_max = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "kaisu_max");
            NativeFieldInfoPtr_face_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "face_no");
            NativeFieldInfoPtr_name = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "name");
            NativeFieldInfoPtr_attack = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "attack");
            NativeFieldInfoPtr_defense = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "defense");
        }

        public WAR_CHARA_TYPE(System.IntPtr pointer) : base(pointer) { }


        // Special abilities array
        public unsafe Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY> nouryoku
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_nouryoku);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<war_data_h.tagSPECIAL_ABILITY>(arrayPtr) : null;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_nouryoku);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                Il2CppInterop.Runtime.IL2CPP.il2cpp_gc_wbarrier_set_field(basePtr, fieldPtr, 
                    Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(value));
            }
        }

        // Usage counts
        public unsafe Il2CppStructArray<byte> kaisu
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_kaisu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(arrayPtr) : null;
            }
        }

        // Max usage counts
        public unsafe Il2CppStructArray<byte> kaisu_max
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_kaisu_max);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                System.IntPtr arrayPtr = *(System.IntPtr*)fieldPtr;
                return (arrayPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(arrayPtr) : null;
            }
        }

        // Character name (string table index)
        public unsafe int name
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_name);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
        }

        // Face portrait index
        public unsafe byte face_no
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_face_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
        }

        // Combat stats
        public unsafe byte attack
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_attack);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_attack);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

        public unsafe byte defense
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_defense);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_defense);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

    }
}
