using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using Eco.Shared.Math;

namespace Gravity;

[ChatCommandHandler]
public static class GravityChatCommand
{
    [ChatCommand("Shows commands for Gravity manipulation.")]
    public static void Gravity(User user) { }

    [ChatSubCommand("Gravity", "Check for integrity and display color on blocks", ChatAuthorizationLevel.Admin)]
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
    }
}

