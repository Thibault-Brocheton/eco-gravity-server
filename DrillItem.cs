namespace Eco.Mods.TechTree
{
    using Eco.Gameplay.Items;
    using Eco.Gameplay.Players;
    using Eco.Shared.Math;
    using Eco.Gameplay.Gravity;
    using Eco.Gameplay.Interactions.Interactors;
    using Eco.Shared.SharedTypes;

    public abstract partial class DrillItem : ToolItem
    {
        [Interaction(InteractionTrigger.RightClick)]
        public bool CheckIntegrity(Player player, InteractionTriggerInfo triggerInfo, InteractionTarget target)
        {
            if (target is not { IsBlock: true, BlockPosition: not null } || GravityService.WorldIntegrities is null) return false;

            if (WrappedWorldPosition3i.TryCreate(target.BlockPosition.Value, out var vec))
            {
                var integrity = GravityService.CalculateIntegrities(vec, target.Block());

                player.MsgLocStr($"Integrity at {target.BlockPosition} is {integrity}%");

                return true;
            }

            return false;
        }
    }
}
