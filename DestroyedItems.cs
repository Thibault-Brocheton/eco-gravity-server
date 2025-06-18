// Copyright (c) Strange Loop Games. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Eco.Gameplay.Gravity
{
    using Eco.Core.Items;
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Objects;
    using Eco.Shared.Localization;
    using Eco.Shared.Serialization;
    using Eco.Shared.SharedTypes;
    using Eco.World.Blocks;
    using Eco.World.Water;
    using System;

    [Serialized]
    [Solid, Wall]
    public partial class DestroyedMortaredStoneBlock : Block, IRepresentsItem
    {
        public virtual Type RepresentedItemType { get { return typeof(DestroyedMortaredStoneItem); } }
    }

    [Serialized]
    [LocDisplayName("DestroyedMortaredStone")]
    [LocDescription("")]
    [MaxStackSize(20)]
    [Weight(10000)]
    [ResourcePile]
    [Tag("Excavatable")]
    [Tag("Rock")]
    public partial class DestroyedMortaredStoneItem : BlockItem<DestroyedMortaredStoneBlock>
    {
        public override LocString DisplayNamePlural { get { return Localizer.DoStr("DestroyedMortaredStone"); } }

        public override bool CanStickToWalls { get { return false; } }

        private static Type[] blockTypes = new Type[] {
            typeof(DestroyedMortaredStoneStacked1Block),
            typeof(DestroyedMortaredStoneStacked2Block),
            typeof(DestroyedMortaredStoneStacked3Block),
            typeof(DestroyedMortaredStoneStacked4Block)
        };

        public override Type[] BlockTypes { get { return blockTypes; } }
    }

    [Tag(BlockTags.PartialStack)]
    [Tag("Excavatable")]
    [Tag("Rock")]
    [Serialized, Solid] public class DestroyedMortaredStoneStacked1Block : PickupableBlock, IWaterLoggedBlock { }

    [Tag(BlockTags.PartialStack)]
    [Tag("Excavatable")]
    [Tag("Rock")]
    [Serialized, Solid] public class DestroyedMortaredStoneStacked2Block : PickupableBlock, IWaterLoggedBlock { }

    [Tag(BlockTags.PartialStack)]
    [Tag("Excavatable")]
    [Tag("Rock")]
    [Serialized, Solid] public class DestroyedMortaredStoneStacked3Block : PickupableBlock, IWaterLoggedBlock { }

    [Tag(BlockTags.FullStack)]
    [Tag("Excavatable")]
    [Tag("Rock")]
    [Serialized, Solid, Wall] public class DestroyedMortaredStoneStacked4Block : PickupableBlock, IWaterLoggedBlock { } //Only a wall if it's all 4 DestroyedMortaredStone

}
