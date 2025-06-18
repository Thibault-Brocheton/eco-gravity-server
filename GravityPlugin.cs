// Copyright (c) Strange Loop Games. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Eco.Gameplay.Gravity
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Plugins;
    using Eco.Core.Utils;
    using Eco.Shared.Logging;
    using Eco.Shared.Utils;
    using Eco.World;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class GravityMod: IModInit
    {
        public static ModRegistration Register() => new()
        {
            ModName = "Gravity",
            ModDescription = "Gravity activates structural integrity on all blocks.",
            ModDisplayName = "Gravity"
        };
    }

    public class GravityConfig : Singleton<GravityConfig>
    {
        public bool GravityEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        public double DefaultMaxResistance { get; set; } = 6;
        public double DefaultMaxOverhang { get; set; } = 3;
        public double DefaultWeight { get; set; } = 1;

        public double MaxDepth { get; set; } = 5;
        public double SupportDistanceForMaxEfficiency { get; set; } = 10;

        public Dictionary<string, IntegrityConfig> PhysicConfiguration { get; set; } = new Dictionary<string, IntegrityConfig>()
        {
            // Algorithmically, all Minable blocks have Infinite Resistance
            // Rocks
            { "SandstoneItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "LimestoneItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "GraniteItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "BasaltItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "GneissItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "ShaleItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            // Special Rocks
            { "CoalItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "SulfurItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            // Ores
            { "IronOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CopperOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "GoldOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            // Crushed Rocks
            { "CrushedSandstoneItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedLimestoneItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedGraniteItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedBasaltItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedGneissItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedShaleItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedCoalItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedSulfurItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedIronOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedCopperOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedGoldOreItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedMixedRockItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            { "CrushedSlagItem", new IntegrityConfig(int.MaxValue, 10, 0) },
            // Dirts
            { "DirtItem", new IntegrityConfig(15, 1, 0) },
            { "SandItem", new IntegrityConfig(8, 1, 0) },
            { "ClayItem", new IntegrityConfig(3, 1, 0) },
            // Tailings
            { "TailingsItem", new IntegrityConfig(int.MaxValue, 1, 0) },
            { "WetTailingsItem", new IntegrityConfig(int.MaxValue, 1, 0) },
            { "GarbageItem", new IntegrityConfig(int.MaxValue, 1, 0) },
            // Construction Blocks
            { "AdobeItem", new IntegrityConfig(6, 3, 1) },
            { "HewnLogItem", new IntegrityConfig(8, 6, 1) },
            { "SoftwoodHewnLogItem", new IntegrityConfig(7, 8, 1) },
            { "HardwoodHewnLogItem", new IntegrityConfig(10, 5, 1) },
            { "MortaredStoneItem", new IntegrityConfig(30, 5, 3) },
            // Construction Forms
            { "FloorType", new IntegrityConfig(0, 1.2, 0) },
            { "SimpleFloorType", new IntegrityConfig(0, 1.2, 0) },
            { "FlatRoofType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofCornerType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofCubeType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofEdgeCornerType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofEdgeSideType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofEdgeTurnType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofEndType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofFillType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofMidType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofPeakType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofPeakCornerType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofPeakSetType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofPeakTType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofPeakXType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofSideType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofSoloType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofTType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofTurnType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofUnderslopeCornerType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofUnderslopeSideType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofUnderslopeTurnType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "RoofXType", new IntegrityConfig(-1.2, 1.2, 0) },
            { "Column", new IntegrityConfig(1.2, -1.2, 0) },
            { "Column_01", new IntegrityConfig(1.2, -1.2, 0) },
            { "Column_02", new IntegrityConfig(1.2, -1.2, 0) },
            { "Column_03", new IntegrityConfig(1.2, -1.2, 0) },
            { "Column_05", new IntegrityConfig(1.2, -1.2, 0) },
        };
    }

    [Priority(PriorityAttribute.VeryLow)]
    public class GravityPlugin : Singleton<GravityPlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin, IShutdownablePlugin
    {
        public static ThreadSafeAction OnSettingsChanged = new();
        public IPluginConfig PluginConfig => this.config;
        private readonly PluginConfig<GravityConfig> config;
        public GravityConfig Config => this.config.Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

        private bool isActivated;

        public GravityPlugin()
        {
            this.config = new PluginConfig<GravityConfig>("Gravity");
            this.SaveConfig();
        }

        public string GetStatus()
        {
            return "OK";
        }

        public string GetCategory()
        {
            return "Mods";
        }

        public void Initialize(TimedTask timer)
        {
            Log.WriteLineLoc($"[GravityMod] Activate World OnBlockChanged");

            if (Obj.Config.GravityEnabled)
            {
                this.ActivateGravity();
            }
        }

        public bool ToggleGravity()
        {
            if (this.isActivated)
            {
                this.DeActivateGravity();
            }
            else
            {
                this.ActivateGravity();
            }

            return this.isActivated;
        }

        private void ActivateGravity()
        {
            if (this.isActivated) return;
            GravityService.ResetWorldIntegrities();
            World.OnBlockChanged.Add(GravityService.HandleBlockChange);

            this.isActivated = true;
        }

        private void DeActivateGravity()
        {
            if (!this.isActivated) return;
            World.OnBlockChanged.Remove(GravityService.HandleBlockChange);

            this.isActivated = false;
        }

        public Task ShutdownAsync()
        {
            this.DeActivateGravity();

            return Task.CompletedTask;
        }

        public object GetEditObject() => this.config.Config;

        public void OnEditObjectChanged(object o, string param)
        {
            this.SaveConfig();
        }
    }
}
