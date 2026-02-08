using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    /// <summary>
    /// War System - Global state and control for war battles
    /// Tracks turn order, special ability usage, forces, and battle status
    /// </summary>
    public class war_sys : Il2CppSystem.Object
    {
        private static readonly System.IntPtr NativeFieldInfoPtr_turn;
        private static readonly System.IntPtr NativeFieldInfoPtr_help_mode;
        private static readonly System.IntPtr NativeFieldInfoPtr_help_timer;
        private static readonly System.IntPtr NativeFieldInfoPtr_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_event_timer;
        private static readonly System.IntPtr NativeFieldInfoPtr_event_act_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_help_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_no_key_time;
        private static readonly System.IntPtr NativeFieldInfoPtr_no_act_time;
        private static readonly System.IntPtr NativeFieldInfoPtr_danmatu_force;
        private static readonly System.IntPtr NativeFieldInfoPtr_danmatu_unit_number;
        private static readonly System.IntPtr NativeFieldInfoPtr_danmatu_chara;
        private static readonly System.IntPtr NativeFieldInfoPtr_sibou_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_force;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_unit;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_chara;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_nou;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_type;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_target_force;
        private static readonly System.IntPtr NativeFieldInfoPtr_tokusyu_target_unit;
        private static readonly System.IntPtr NativeFieldInfoPtr_map;
        private static readonly System.IntPtr NativeFieldInfoPtr_end_event_no;
        private static readonly System.IntPtr NativeFieldInfoPtr_field_disp_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_cpu_mode_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_cpu_target_x;
        private static readonly System.IntPtr NativeFieldInfoPtr_cpu_target_y;
        private static readonly System.IntPtr NativeFieldInfoPtr_battle_rgb;
        private static readonly System.IntPtr NativeFieldInfoPtr_team_data_disp_flag;
        private static readonly System.IntPtr NativeFieldInfoPtr_attack_force;
        private static readonly System.IntPtr NativeFieldInfoPtr_attack_unit;
        private static readonly System.IntPtr NativeFieldInfoPtr_defense_force;
        private static readonly System.IntPtr NativeFieldInfoPtr_defense_unit;
        private static readonly System.IntPtr NativeFieldInfoPtr_game_over_flag;

        static war_sys()
        {
            Il2CppClassPointerStore<war_sys>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "war_sys");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<war_sys>.NativeClassPtr);
            
            NativeFieldInfoPtr_turn = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "turn");
            NativeFieldInfoPtr_help_mode = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "help_mode");
            NativeFieldInfoPtr_help_timer = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "help_timer");
            NativeFieldInfoPtr_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "flag");
            NativeFieldInfoPtr_event_timer = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "event_timer");
            NativeFieldInfoPtr_event_act_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "event_act_flag");
            NativeFieldInfoPtr_help_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "help_no");
            NativeFieldInfoPtr_no_key_time = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "no_key_time");
            NativeFieldInfoPtr_no_act_time = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "no_act_time");
            NativeFieldInfoPtr_danmatu_force = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "danmatu_force");
            NativeFieldInfoPtr_danmatu_unit_number = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "danmatu_unit_number");
            NativeFieldInfoPtr_danmatu_chara = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "danmatu_chara");
            NativeFieldInfoPtr_sibou_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "sibou_flag");
            NativeFieldInfoPtr_tokusyu_force = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_force");
            NativeFieldInfoPtr_tokusyu_unit = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_unit");
            NativeFieldInfoPtr_tokusyu_chara = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_chara");
            NativeFieldInfoPtr_tokusyu_nou = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_nou");
            NativeFieldInfoPtr_tokusyu_type = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_type");
            NativeFieldInfoPtr_tokusyu_target_force = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_target_force");
            NativeFieldInfoPtr_tokusyu_target_unit = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "tokusyu_target_unit");
            NativeFieldInfoPtr_map = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "map");
            NativeFieldInfoPtr_end_event_no = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "end_event_no");
            NativeFieldInfoPtr_field_disp_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "field_disp_flag");
            NativeFieldInfoPtr_cpu_mode_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "cpu_mode_flag");
            NativeFieldInfoPtr_cpu_target_x = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "cpu_target_x");
            NativeFieldInfoPtr_cpu_target_y = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "cpu_target_y");
            NativeFieldInfoPtr_battle_rgb = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "battle_rgb");
            NativeFieldInfoPtr_team_data_disp_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "team_data_disp_flag");
            NativeFieldInfoPtr_attack_force = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "attack_force");
            NativeFieldInfoPtr_attack_unit = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "attack_unit");
            NativeFieldInfoPtr_defense_force = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "defense_force");
            NativeFieldInfoPtr_defense_unit = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "defense_unit");
            NativeFieldInfoPtr_game_over_flag = Il2CppInterop.Runtime.IL2CPP.GetIl2CppField(Il2CppClassPointerStore<war_sys>.NativeClassPtr, "game_over_flag");
        }

        public war_sys(System.IntPtr pointer) : base(pointer) { }

        // Battle turn tracking
        public unsafe static long turn
        {
            get
            {
                long num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_turn, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_turn, (void*)(&value));
            }
        }

        public unsafe static byte help_mode
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_help_mode, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_help_mode, (void*)(&value));
            }
        }

        public unsafe static uint help_timer
        {
            get
            {
                uint num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_help_timer, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_help_timer, (void*)(&value));
            }
        }

        // Battle flags array
        public unsafe static Il2CppStructArray<byte> flag
        {
            get
            {
                System.IntPtr intPtr;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_flag, (void*)(&intPtr));
                return (intPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(intPtr) : null;
            }
            set
            {
                System.IntPtr ptr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(value);
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_flag, (void*)ptr);
            }
        }

        public unsafe static byte event_timer
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_event_timer, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_event_timer, (void*)(&value));
            }
        }

        public unsafe static sbyte event_act_flag
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_event_act_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_event_act_flag, (void*)(&value));
            }
        }

        public unsafe static byte help_no
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_help_no, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_help_no, (void*)(&value));
            }
        }

        public unsafe static ushort no_key_time
        {
            get
            {
                ushort num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_no_key_time, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_no_key_time, (void*)(&value));
            }
        }

        public unsafe static ushort no_act_time
        {
            get
            {
                ushort num;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_no_act_time, (void*)(&num));
                return num;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_no_act_time, (void*)(&value));
            }
        }

        // Casualty tracking (danmatu = casualty)
        public unsafe static sbyte danmatu_force
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_danmatu_force, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_danmatu_force, (void*)(&value));
            }
        }

        public unsafe static byte danmatu_unit_number
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_danmatu_unit_number, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_danmatu_unit_number, (void*)(&value));
            }
        }

        public unsafe static byte danmatu_chara
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_danmatu_chara, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_danmatu_chara, (void*)(&value));
            }
        }

        public unsafe static byte sibou_flag
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_sibou_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_sibou_flag, (void*)(&value));
            }
        }

        // Special ability (tokusyu) tracking
        public unsafe static byte tokusyu_force
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_force, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_force, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_unit
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_unit, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_unit, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_chara
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_chara, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_chara, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_nou
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_nou, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_nou, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_type
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_type, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_type, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_target_force
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_target_force, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_target_force, (void*)(&value));
            }
        }

        public unsafe static byte tokusyu_target_unit
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_tokusyu_target_unit, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_tokusyu_target_unit, (void*)(&value));
            }
        }

        // Battle map state
        public unsafe static Il2CppStructArray<byte> map
        {
            get
            {
                System.IntPtr intPtr;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_map, (void*)(&intPtr));
                return (intPtr != System.IntPtr.Zero) ? new Il2CppStructArray<byte>(intPtr) : null;
            }
            set
            {
                System.IntPtr ptr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(value);
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_map, (void*)ptr);
            }
        }

        public unsafe static byte end_event_no
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_end_event_no, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_end_event_no, (void*)(&value));
            }
        }

        // Display and UI flags
        public unsafe static sbyte field_disp_flag
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_field_disp_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_field_disp_flag, (void*)(&value));
            }
        }

        public unsafe static sbyte cpu_mode_flag
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_cpu_mode_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_cpu_mode_flag, (void*)(&value));
            }
        }

        public unsafe static byte cpu_target_x
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_cpu_target_x, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_cpu_target_x, (void*)(&value));
            }
        }

        public unsafe static byte cpu_target_y
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_cpu_target_y, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_cpu_target_y, (void*)(&value));
            }
        }

        public unsafe static byte battle_rgb
        {
            get
            {
                byte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_battle_rgb, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_battle_rgb, (void*)(&value));
            }
        }

        public unsafe static sbyte team_data_disp_flag
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_team_data_disp_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_team_data_disp_flag, (void*)(&value));
            }
        }

        // Force and unit tracking (current attacker/defender)
        public unsafe static sbyte attack_force
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_attack_force, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_attack_force, (void*)(&value));
            }
        }

        public unsafe static sbyte attack_unit
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_attack_unit, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_attack_unit, (void*)(&value));
            }
        }

        public unsafe static sbyte defense_force
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_defense_force, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_defense_force, (void*)(&value));
            }
        }

        public unsafe static sbyte defense_unit
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_defense_unit, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_defense_unit, (void*)(&value));
            }
        }

        public unsafe static sbyte game_over_flag
        {
            get
            {
                sbyte b;
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_game_over_flag, (void*)(&b));
                return b;
            }
            set
            {
                Il2CppInterop.Runtime.IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_game_over_flag, (void*)(&value));
            }
        }
    }
}
