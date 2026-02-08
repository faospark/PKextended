using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    /// <summary>
    /// War character data structure - individual unit in war battles
    /// Contains base stats, abilities, composition bonuses, and metadata
    /// </summary>
    public class WAR_CHARA_TYPE : Il2CppSystem.Object
    {
        private static readonly System.IntPtr NativeFieldInfoPtr_force_type;
        private static readonly System.IntPtr NativeFieldInfoPtr_nouryoku;
        private static readonly System.IntPtr NativeFieldInfoPtr_kaisu;
        private static readonly System.IntPtr NativeFieldInfoPtr_kaisu_max;
        private static readonly System.IntPtr NativeFieldInfoPtr_face_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_name;
        private static readonly System.IntPtr NativeFieldInfoPtr_big_name;
        private static readonly System.IntPtr NativeFieldInfoPtr_attack;
        private static readonly System.IntPtr NativeFieldInfoPtr_assist_attack;
        private static readonly System.IntPtr NativeFieldInfoPtr_defense;
        private static readonly System.IntPtr NativeFieldInfoPtr_assist_defense;
        private static readonly System.IntPtr NativeFieldInfoPtr_magic_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_status_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_yarare_serifu;
        private static readonly System.IntPtr NativeFieldInfoPtr_sibou_serifu;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_serifu1;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_serifu2;


        static WAR_CHARA_TYPE()
        {
            Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "WAR_CHARA_TYPE");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr);
            
            NativeFieldInfoPtr_force_type = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "force_type");
            NativeFieldInfoPtr_nouryoku = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "nouryoku");
            NativeFieldInfoPtr_kaisu = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "kaisu");
            NativeFieldInfoPtr_kaisu_max = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "kaisu_max");
            NativeFieldInfoPtr_face_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "face_no");
            NativeFieldInfoPtr_name = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "name");
            NativeFieldInfoPtr_big_name = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "big_name");
            NativeFieldInfoPtr_attack = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "attack");
            NativeFieldInfoPtr_assist_attack = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "assist_attack");
            NativeFieldInfoPtr_defense = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "defense");
            NativeFieldInfoPtr_assist_defense = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "assist_defense");
            NativeFieldInfoPtr_magic_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "magic_no");
            NativeFieldInfoPtr_status_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "status_flag");
            NativeFieldInfoPtr_yarare_serifu = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "yarare_serifu");
            NativeFieldInfoPtr_sibou_serifu = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "sibou_serifu");
            NativeFieldInfoPtr_tokusyu_serifu1 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "tokusyu_serifu1");
            NativeFieldInfoPtr_tokusyu_serifu2 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<WAR_CHARA_TYPE>.NativeClassPtr, "tokusyu_serifu2");
        }

        public WAR_CHARA_TYPE(System.IntPtr pointer) : base(pointer) { }

        // Force affiliation
        public unsafe byte force_type
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_force_type);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_force_type);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

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

        // Ability usage counts
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
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_kaisu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                Il2CppInterop.Runtime.IL2CPP.il2cpp_gc_wbarrier_set_field(basePtr, fieldPtr,
                    Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(value));
            }
        }

        // Maximum ability usage counts
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
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_kaisu_max);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                Il2CppInterop.Runtime.IL2CPP.il2cpp_gc_wbarrier_set_field(basePtr, fieldPtr,
                    Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(value));
            }
        }

        // Character identification
        public unsafe byte face_no
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_face_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_face_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

        public unsafe int name
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_name);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_name);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }

        public unsafe int big_name
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_big_name);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_big_name);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }

        // Combat stats - Base values
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

        /// <summary>
        /// Bonus attack from subunits/composition. Added to base attack.
        /// Example: Main unit attack=8, assist_attack=2 → total attack = 10
        /// </summary>
        public unsafe byte assist_attack
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_assist_attack);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_assist_attack);
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

        /// <summary>
        /// Bonus defense from subunits/composition. Added to base defense.
        /// Example: Main unit defense=9, assist_defense=1 → total defense = 10
        /// </summary>
        public unsafe byte assist_defense
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_assist_defense);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_assist_defense);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

        public unsafe byte magic_no
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_magic_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_magic_no);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

        public unsafe byte status_flag
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_status_flag);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(byte*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_status_flag);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(byte*)fieldPtr = value;
            }
        }

        // Dialog/speech references
        public unsafe int yarare_serifu
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_yarare_serifu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_yarare_serifu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }

        public unsafe int sibou_serifu
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_sibou_serifu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_sibou_serifu);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }

        public unsafe int tokusyu_serifu1
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_tokusyu_serifu1);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_tokusyu_serifu1);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }

        public unsafe int tokusyu_serifu2
        {
            get
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_tokusyu_serifu2);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                return *(int*)fieldPtr;
            }
            set
            {
                System.IntPtr basePtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(this);
                int offset = (int)Il2CppInterop.Runtime.IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr_tokusyu_serifu2);
                System.IntPtr fieldPtr = System.IntPtr.Add(basePtr, offset);
                *(int*)fieldPtr = value;
            }
        }
    }
}
