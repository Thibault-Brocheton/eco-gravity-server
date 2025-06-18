// Copyright (c) Strange Loop Games. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Eco.Mods.TechTree
{
    using Eco.Gameplay.Gravity;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.NewTooltip.TooltipLibraryFiles;
    using Eco.Gameplay.Systems.NewTooltip;
    using Eco.Shared.Items;
    using Eco.Shared.Localization;
    using System;

    //ToolItem can not be refrenced in Eco.Gameplay so its tooltip library should be declared here.
    [TooltipLibrary]
    public static class PhysicConfigurationTooltipLibrary
    {
        public static void Initialize() { }

        [NewTooltip(CacheAs.User | CacheAs.SubType, 200, overrideType: typeof(BlockItem))]
        public static LocString PhysicConfigurationTooltip(Type type, User user, TooltipOrigin origin)
        {
            var item = Item.Get(type) as BlockItem;

            if (item is null || !GravityPlugin.Obj.Config.PhysicConfiguration.TryGetValue(item.GetType().Name, out var physic))
            {
                return LocString.Empty;
            }

            var s = new LocStringBuilder();

            s.AppendLine(Localizer.DoStr($"Weight: {physic.Weight} Kg"));
            s.AppendLine(Localizer.DoStr($"Resistance: {physic.Resistance} Kg"));
            s.AppendLine(Localizer.DoStr($"Overhang: {physic.Overhang} Blocks"));

            return new TooltipSection(Localizer.DoStr($"Gravity Mod [{item.GetType().Name}]:"), s.ToLocString());
        }
    }
}
