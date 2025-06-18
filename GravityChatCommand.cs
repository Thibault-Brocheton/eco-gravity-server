// Copyright (c) Strange Loop Games. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Eco.Gameplay.Gravity
{
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;

    [ChatCommandHandler]
    public static class GravityChatCommand
    {
        [ChatCommand("Shows commands for Gravity manipulation.")]
        public static void Gravity(User user) { }

        [ChatSubCommand("Gravity", "Reset WorldIntegrities", "greset", ChatAuthorizationLevel.Admin)]
        public static void Reset(User user)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                GravityService.ResetWorldIntegrities();
            }

            user.MsgLocStr("Reset successful");
        }

        [ChatSubCommand("Gravity", "Toggle", ChatAuthorizationLevel.Admin)]
        public static void Toggle(User user, bool save = false)
        {
            var isActivated = GravityPlugin.Obj.ToggleGravity();

            if (save)
            {
                GravityPlugin.Obj.Config.GravityEnabled = isActivated;
            }

            user.MsgLocStr($"Gravity is now {(isActivated ? "enabled" : "disabled")}.");
        }

        /*[ChatSubCommand("Gravity", "Check for integrity and display color on blocks", ChatAuthorizationLevel.Admin)]
        public static void Calculate(User user, int x, int y, int z)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                var construction = GravityService.GetConstruction([new Vector3i(x, y, z)]);
                GravityService.MarkBlocks(construction);
            }

            user.MsgLocStr("Calculate successful");
        }

        [ChatSubCommand("Gravity", "Check for integrity and display color on blocks", ChatAuthorizationLevel.Admin)]
        public static void CalculateAround(User user)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                var construction = GravityService.GetConstruction(user.Position.XYZi().XYZNeighbors
                    .Select(n => WrappedWorldPosition3i.TryCreate(n, out var val) ? (WrappedWorldPosition3i?)val : null)
                    .Where(n => n is not null)
                    .OfType<WrappedWorldPosition3i>()
                    .ToList());
                GravityService.MarkBlocks(construction);
            }

            user.MsgLocStr("Calculate successful");
        }

        [ChatSubCommand("Gravity", "Clear color integrity", ChatAuthorizationLevel.Admin)]
        public static void ClearColor(User user)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                GravityService.ClearColorIntegrities();
            }

            user.MsgLocStr("Colors cleared");
        }

        [ChatSubCommand("Gravity", "Clear world integrities", ChatAuthorizationLevel.Admin)]
        public static void ClearWorldIntegrities(User user)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                GravityService.ClearWorldIntegrities();
            }

            user.MsgLocStr("World integrities cleared");
        }

        [ChatSubCommand("Gravity", "Display world integrities", ChatAuthorizationLevel.Admin)]
        public static void DisplayWorldIntegrities(User user)
        {
            if (GravityPlugin.Obj.Config.GravityEnabled)
            {
                GravityService.DisplayWorldIntegrities();
            }

            user.MsgLocStr("World integrities displayed");
        }*/
    }
}
