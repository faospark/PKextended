using System;
using Il2CppInterop.Runtime;
using Il2CppSystem;

namespace PKCore.IL2CPP
{
    public class war_data_h
    {
        static war_data_h()
        {
            Il2CppClassPointerStore<war_data_h>.NativeClassPtr = Il2CppInterop.Runtime.IL2CPP.GetIl2CppClass("GSD2.dll", "", "war_data_h");
            Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<war_data_h>.NativeClassPtr);
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
