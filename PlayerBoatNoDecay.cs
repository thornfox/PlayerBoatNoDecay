using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rust;

namespace Oxide.Plugins
{
    [Info("Player Boat No Decay", "ThornFox", "1.1.0")]
    [Description("Prevents player-built (modular) boats from taking decay damage.")]
    public class PlayerBoatNoDecay : RustPlugin
    {
        private const string PermUse = "playerboatnodecay.use";
        private const int MaxParentDepth = 10;

        private Configuration config;
        private HashSet<string> boatTypeNames;

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Only protect boats of players with permission (false = protect all player boats)")]
            public bool RequirePermission = false;

            [JsonProperty("Protected entity types (class name or short prefab name)")]
            public List<string> ProtectedTypes = new List<string> { "PlayerBoat" };

            [JsonProperty("Debug output in console")]
            public bool Debug = false;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception("Configuration is empty");
                SaveConfig();
            }
            catch (Exception ex)
            {
                PrintError($"Configuration is invalid, using default values: {ex.Message}");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermUse, this);
            boatTypeNames = new HashSet<string>(config.ProtectedTypes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            // Early exit: we only care about pure decay damage
            if (entity == null || info == null || info.damageTypes == null) return null;
            if (!IsDecayOnly(info.damageTypes)) return null;

            BaseEntity boat = FindPlayerBoat(entity);
            if (boat == null) return null;

            if (config.RequirePermission && !OwnerHasPermission(boat, entity)) return null;

            if (config.Debug)
                Puts($"Blocked decay on {entity.ShortPrefabName} ({entity.net?.ID}) attached to boat {boat.ShortPrefabName}.");

            return true; // Damage is discarded
        }

        #endregion

        #region Helpers

        // True if the damage consists entirely (or almost entirely) of decay
        private static bool IsDecayOnly(DamageTypeList damage)
        {
            float decay = damage.Get(DamageType.Decay);
            if (decay <= 0f) return false;
            return decay >= damage.Total() * 0.99f;
        }

        // Checks the entity itself and its parent chain (building blocks/deployables are parented to the boat)
        private BaseEntity FindPlayerBoat(BaseEntity entity)
        {
            BaseEntity current = entity;
            for (int i = 0; i < MaxParentDepth && current != null; i++)
            {
                if (IsProtectedType(current)) return current;
                current = current.GetParentEntity();
            }
            return null;
        }

        private bool IsProtectedType(BaseEntity entity)
        {
            if (!string.IsNullOrEmpty(entity.ShortPrefabName) && boatTypeNames.Contains(entity.ShortPrefabName))
                return true;

            // Walk the class hierarchy so derived boat classes are recognized as well
            for (Type t = entity.GetType(); t != null && t != typeof(BaseEntity); t = t.BaseType)
            {
                if (boatTypeNames.Contains(t.Name)) return true;
            }
            return false;
        }

        private bool OwnerHasPermission(BaseEntity boat, BaseEntity part)
        {
            ulong owner = boat.OwnerID != 0 ? boat.OwnerID : part.OwnerID;
            if (owner == 0) return false;
            return permission.UserHasPermission(owner.ToString(), PermUse);
        }

        #endregion
    }
}
