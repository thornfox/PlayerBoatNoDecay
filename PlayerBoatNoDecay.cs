using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rust;

namespace Oxide.Plugins
{
    [Info("Player Boat No Decay", "ThornFox", "1.2.0")]
    [Description("Prevents player-built (modular) boats from taking decay damage.")]
    public class PlayerBoatNoDecay : RustPlugin
    {
        private const string PermUse = "playerboatnodecay.use";
        private const int MaxParentDepth = 10;

        private Configuration config;
        private HashSet<string> boatTypeNames;

        // Cache: prefabID -> "is this prefab a protected boat type?" (avoids reflection on every decay tick)
        private readonly Dictionary<uint, bool> protectedPrefabCache = new Dictionary<uint, bool>();

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

            if (config.RequirePermission && !AnyOwnerHasPermission(boat, entity)) return null;

            if (config.Debug)
                Puts($"Blocked decay on {entity.ShortPrefabName} ({entity.net?.ID}) attached to boat {boat.ShortPrefabName} ({boat.net?.ID}).");

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

        // Checks the entity itself and its parent chain (building blocks/deployables are parented to the boat).
        // Stops at any other vehicle, so vehicles parked on a boat (e.g. a minicopter on deck) keep their vanilla decay.
        private BaseEntity FindPlayerBoat(BaseEntity entity)
        {
            BaseEntity current = entity;
            for (int i = 0; i < MaxParentDepth && current != null; i++)
            {
                if (IsProtectedType(current)) return current;
                if (current is BaseVehicle) return null;
                current = current.GetParentEntity();
            }
            return null;
        }

        private bool IsProtectedType(BaseEntity entity)
        {
            bool result;
            if (protectedPrefabCache.TryGetValue(entity.prefabID, out result)) return result;

            result = MatchesProtectedType(entity);
            protectedPrefabCache[entity.prefabID] = result;
            return result;
        }

        private bool MatchesProtectedType(BaseEntity entity)
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

        // Vanilla Rust does not assign an OwnerID to player-built boats, so ownership is resolved via:
        // 1. the boat's OwnerID (if set by another plugin),
        // 2. every player authorized on the boat's vehicle privilege (boat cupboard),
        // 3. the OwnerID of the decaying part itself (e.g. the player who placed a deployable).
        private bool AnyOwnerHasPermission(BaseEntity boat, BaseEntity part)
        {
            if (HasPerm(boat.OwnerID)) return true;

            VehiclePrivilege privilege = GetVehiclePrivilege(boat);
            if (privilege != null && privilege.authorizedPlayers != null)
            {
                foreach (var userId in privilege.authorizedPlayers)
                {
                    if (HasPerm(userId)) return true;
                }
            }

            return part != boat && HasPerm(part.OwnerID);
        }

        private bool HasPerm(ulong userId) =>
            userId != 0 && permission.UserHasPermission(userId.ToString(), PermUse);

        private static VehiclePrivilege GetVehiclePrivilege(BaseEntity boat)
        {
            if (boat.children == null) return null;

            foreach (BaseEntity child in boat.children)
            {
                VehiclePrivilege privilege = child as VehiclePrivilege;
                if (privilege != null) return privilege;
            }

            foreach (BaseEntity child in boat.children)
            {
                SteeringWheel wheel = child as SteeringWheel;
                if (wheel != null && wheel.Privilege != null) return wheel.Privilege;
            }

            return null;
        }

        #endregion
    }
}
