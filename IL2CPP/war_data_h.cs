using System;
using Il2CppInterop.Runtime;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    public class war_data_h : Il2CppSystem.Object
    {
        private static readonly System.IntPtr NativeFieldInfoPtr_INFANTRY;
        private static readonly System.IntPtr NativeFieldInfoPtr_ARCHER;
        private static readonly System.IntPtr NativeFieldInfoPtr_MAGICIANS;
        private static readonly System.IntPtr NativeFieldInfoPtr_MAGIC_THUNDER;
        private static readonly System.IntPtr NativeFieldInfoPtr_MAGIC_FIRE;
        private static readonly System.IntPtr NativeFieldInfoPtr_MAGIC_WIND;
        private static readonly System.IntPtr NativeFieldInfoPtr_DEAD_POSSIBLE;
        private static readonly System.IntPtr NativeFieldInfoPtr_DEAD_UNPOSSIBLE;
        private static readonly System.IntPtr NativeFieldInfoPtr_ANNIHILATION_UNPOSSIBLE;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_WAIT;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_ON_STAGE;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_DESTROY;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_PRE_DESTROY;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_PRE_ON_STAGE;
        private static readonly System.IntPtr NativeFieldInfoPtr_UNIT_FLAG_PRE_WAIT;
        private static readonly System.IntPtr NativeFieldInfoPtr_KEKKA_FLG_DEFENSE_DAMAGE;
        private static readonly System.IntPtr NativeFieldInfoPtr_KEKKA_FLG_ATTACK_DAMAGE;
        private static readonly System.IntPtr NativeFieldInfoPtr_KEKKA_FLG_KANSETU_ATTACK;
        private static readonly System.IntPtr NativeFieldInfoPtr_KEKKA_FLG_KANSETU_DEFENSE;
        private static readonly System.IntPtr NativeFieldInfoPtr_SP_TARGET_SELECT_NONE;
        private static readonly System.IntPtr NativeFieldInfoPtr_SP_TARGET_SELECT_0;
        private static readonly System.IntPtr NativeFieldInfoPtr_SP_TARGET_SELECT_1;
        private static readonly System.IntPtr NativeFieldInfoPtr_SP_TARGET_SELECT_2;
        private static readonly System.IntPtr NativeFieldInfoPtr_SP_TARGET_SELECT_3;

        static war_data_h()
        {
            Il2CppClassPointerStore<war_data_h>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "war_data_h");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<war_data_h>.NativeClassPtr);
            
            NativeFieldInfoPtr_INFANTRY = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "INFANTRY");
            NativeFieldInfoPtr_ARCHER = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "ARCHER");
            NativeFieldInfoPtr_MAGICIANS = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "MAGICIANS");
            NativeFieldInfoPtr_MAGIC_THUNDER = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "MAGIC_THUNDER");
            NativeFieldInfoPtr_MAGIC_FIRE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "MAGIC_FIRE");
            NativeFieldInfoPtr_MAGIC_WIND = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "MAGIC_WIND");
            NativeFieldInfoPtr_DEAD_POSSIBLE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "DEAD_POSSIBLE");
            NativeFieldInfoPtr_DEAD_UNPOSSIBLE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "DEAD_UNPOSSIBLE");
            NativeFieldInfoPtr_ANNIHILATION_UNPOSSIBLE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "ANNIHILATION_UNPOSSIBLE");
            NativeFieldInfoPtr_UNIT_FLAG_WAIT = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_WAIT");
            NativeFieldInfoPtr_UNIT_FLAG_ON_STAGE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_ON_STAGE");
            NativeFieldInfoPtr_UNIT_FLAG_DESTROY = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_DESTROY");
            NativeFieldInfoPtr_UNIT_FLAG_PRE_DESTROY = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_PRE_DESTROY");
            NativeFieldInfoPtr_UNIT_FLAG_PRE_ON_STAGE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_PRE_ON_STAGE");
            NativeFieldInfoPtr_UNIT_FLAG_PRE_WAIT = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "UNIT_FLAG_PRE_WAIT");
            NativeFieldInfoPtr_KEKKA_FLG_DEFENSE_DAMAGE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "KEKKA_FLG_DEFENSE_DAMAGE");
            NativeFieldInfoPtr_KEKKA_FLG_ATTACK_DAMAGE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "KEKKA_FLG_ATTACK_DAMAGE");
            NativeFieldInfoPtr_KEKKA_FLG_KANSETU_ATTACK = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "KEKKA_FLG_KANSETU_ATTACK");
            NativeFieldInfoPtr_KEKKA_FLG_KANSETU_DEFENSE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "KEKKA_FLG_KANSETU_DEFENSE");
            NativeFieldInfoPtr_SP_TARGET_SELECT_NONE = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "SP_TARGET_SELECT_NONE");
            NativeFieldInfoPtr_SP_TARGET_SELECT_0 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "SP_TARGET_SELECT_0");
            NativeFieldInfoPtr_SP_TARGET_SELECT_1 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "SP_TARGET_SELECT_1");
            NativeFieldInfoPtr_SP_TARGET_SELECT_2 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "SP_TARGET_SELECT_2");
            NativeFieldInfoPtr_SP_TARGET_SELECT_3 = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_data_h>.NativeClassPtr, "SP_TARGET_SELECT_3");
        }

        public war_data_h(System.IntPtr pointer) : base(pointer) { }

        // Unit type constants
        public unsafe static byte INFANTRY
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_INFANTRY, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_INFANTRY, (void*)(&value));
            }
        }

        public unsafe static byte ARCHER
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_ARCHER, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_ARCHER, (void*)(&value));
            }
        }

        public unsafe static byte MAGICIANS
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_MAGICIANS, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_MAGICIANS, (void*)(&value));
            }
        }

        public unsafe static byte MAGIC_THUNDER
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_MAGIC_THUNDER, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_MAGIC_THUNDER, (void*)(&value));
            }
        }

        public unsafe static byte MAGIC_FIRE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_MAGIC_FIRE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_MAGIC_FIRE, (void*)(&value));
            }
        }

        public unsafe static byte MAGIC_WIND
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_MAGIC_WIND, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_MAGIC_WIND, (void*)(&value));
            }
        }

        // Battle status flags
        public unsafe static byte DEAD_POSSIBLE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_DEAD_POSSIBLE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_DEAD_POSSIBLE, (void*)(&value));
            }
        }

        public unsafe static byte DEAD_UNPOSSIBLE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_DEAD_UNPOSSIBLE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_DEAD_UNPOSSIBLE, (void*)(&value));
            }
        }

        public unsafe static byte ANNIHILATION_UNPOSSIBLE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_ANNIHILATION_UNPOSSIBLE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_ANNIHILATION_UNPOSSIBLE, (void*)(&value));
            }
        }

        // Unit state flags
        public unsafe static int UNIT_FLAG_WAIT
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_WAIT, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_WAIT, (void*)(&value));
            }
        }

        public unsafe static int UNIT_FLAG_ON_STAGE
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_ON_STAGE, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_ON_STAGE, (void*)(&value));
            }
        }

        public unsafe static int UNIT_FLAG_DESTROY
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_DESTROY, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_DESTROY, (void*)(&value));
            }
        }

        public unsafe static int UNIT_FLAG_PRE_DESTROY
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_DESTROY, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_DESTROY, (void*)(&value));
            }
        }

        public unsafe static int UNIT_FLAG_PRE_ON_STAGE
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_ON_STAGE, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_ON_STAGE, (void*)(&value));
            }
        }

        public unsafe static int UNIT_FLAG_PRE_WAIT
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_WAIT, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_UNIT_FLAG_PRE_WAIT, (void*)(&value));
            }
        }

        // Battle result flags
        public unsafe static byte KEKKA_FLG_DEFENSE_DAMAGE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_KEKKA_FLG_DEFENSE_DAMAGE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_KEKKA_FLG_DEFENSE_DAMAGE, (void*)(&value));
            }
        }

        public unsafe static byte KEKKA_FLG_ATTACK_DAMAGE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_KEKKA_FLG_ATTACK_DAMAGE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_KEKKA_FLG_ATTACK_DAMAGE, (void*)(&value));
            }
        }

        public unsafe static byte KEKKA_FLG_KANSETU_ATTACK
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_KEKKA_FLG_KANSETU_ATTACK, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_KEKKA_FLG_KANSETU_ATTACK, (void*)(&value));
            }
        }

        public unsafe static byte KEKKA_FLG_KANSETU_DEFENSE
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_KEKKA_FLG_KANSETU_DEFENSE, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_KEKKA_FLG_KANSETU_DEFENSE, (void*)(&value));
            }
        }

        // Special ability target selection
        public unsafe static int SP_TARGET_SELECT_NONE
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_SP_TARGET_SELECT_NONE, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_SP_TARGET_SELECT_NONE, (void*)(&value));
            }
        }

        public unsafe static int SP_TARGET_SELECT_0
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_SP_TARGET_SELECT_0, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_SP_TARGET_SELECT_0, (void*)(&value));
            }
        }

        public unsafe static int SP_TARGET_SELECT_1
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_SP_TARGET_SELECT_1, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_SP_TARGET_SELECT_1, (void*)(&value));
            }
        }

        public unsafe static int SP_TARGET_SELECT_2
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_SP_TARGET_SELECT_2, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_SP_TARGET_SELECT_2, (void*)(&value));
            }
        }

        public unsafe static int SP_TARGET_SELECT_3
        {
            get
            {
                int num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_SP_TARGET_SELECT_3, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_SP_TARGET_SELECT_3, (void*)(&value));
            }
        }

        /// <summary>
        /// Tactical evaluation constants for AI decision-making
        /// </summary>
        public enum tagCHECK
        {
            CHECK_MAWARI,
            CHECK_END
        }

        /// <summary>
        /// AI evaluation scores for different tactical situations
        /// </summary>
        public enum tagHYOUKA
        {
            HYOUKA_YARARE,              // Being attacked
            HYOUKA_NEAR_ENEMY,          // Enemy nearby
            HYOUKA_ATTACK,              // Can attack
            HYOUKA_HANGEKI,             // Counter-attack
            HYOUKA_TOKUSYU,             // Special ability available
            HYOUKA_MOVE_MIN,            // Minimal movement
            HYOUKA_MOVE_TO_POINT,       // Move to position
            HYOUKA_MOVE_TO_UNIT,        // Move to unit
            HYOUKA_SP_FLAME_SPEAR,      // Flame Spear ability
            HYOUKA_SP_AIMING,           // Aiming ability
            HYOUKA_SP_MAGIC_WIND1,      // Wind Magic ability
            HYOUKA_SP_MAGIC_THUNDER1,   // Thunder Magic ability
            HYOUKA_SP_MEDICAL2,         // Healing ability
            HYOUKA_SP_SHINING_SHIELD,   // Shield ability
            HYOUKA_SP_MEDICAL1,         // Minor healing
            HYOUKA_SP_CHEAR_UP,         // Morale boost
            HYOUKA_SP_INVENTION,        // Invention ability
            HYOUKA_SP_MAGIC_FIRE1,      // Fire Magic ability
            HYOUKA_HEAL_OCCUPATION,     // Healing through occupation
            HYOUKA_HEAL_MOVE,           // Healing through movement
            HYOUKA_END
        }

        /// <summary>
        /// Unit action types in war battles
        /// </summary>
        public enum tagACTION
        {
            ACT_ATTACK,     // Normal attack
            ACT_NONE,       // No action
            ACT_TOKUSYU     // Special ability action
        }

        public enum tagSPECIAL_ABILITY
        {
            SP_NONE,
            SP_MOUNT,           // Cavalry
            SP_CHARGE,          // Charge attack
            SP_AIMING,          // Precise targeting
            SP_FLAME_SPEAR,     // Fire spear attack
            SP_SHINING_SHIELD,  // Defensive shield
            SP_HP_PLUS,         // HP bonus
            SP_CRITICAL,        // Critical hit chance
            SP_FORCE_MOVE,      // Forced movement
            SP_SCOUT,           // Scouting ability
            SP_FOREST_WALK,     // Move through forests
            SP_MAGIC_WIND1,     // Wind magic level 1
            SP_MAGIC_FIRE1,     // Fire magic level 1
            SP_MAGIC_THUNDER1,  // Thunder magic level 1
            SP_MAGIC_WIND2,     // Wind magic level 2
            SP_SEE_THROUGH,     // See through deception
            SP_KIN_SLAYER,      // Bonus vs kin
            SP_MEDICAL1,        // Healing level 1
            SP_MEDICAL2,        // Healing level 2
            SP_THROUGH_ROAD,    // Move through roads faster
            SP_BODY_GUARD,      // Protect allies
            SP_CHEAR_UP,        // Morale boost
            SP_INVESTIGATION,   // Investigation ability
            SP_INVENTION,       // Special inventions
            SP_CONFUSED_FIGHT,  // Confusion attack
            SP_FLYING           // Flying unit
        }
    }
}
