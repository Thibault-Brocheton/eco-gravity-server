using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Server;
using Eco.Shared.Logging;
using Eco.Shared.Utils;
using Eco.World;

namespace Gravity
{
    public class GravityConfig : Singleton<GravityConfig>
    {
        public bool GravityEnabled { get; set; } = true;
    }

    [Priority(PriorityAttribute.VeryLow)]
    public class GravityPlugin : Singleton<GravityPlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin, IShutdownablePlugin
    {
        public static ThreadSafeAction OnSettingsChanged = new();
        public IPluginConfig PluginConfig => this.config;
        private readonly PluginConfig<GravityConfig> config;
        public GravityConfig Config => this.config.Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

        public GravityPlugin()
        {
            this.config = new PluginConfig<GravityConfig>("Gravity");
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
            PluginManager.Obj.InitComplete += () =>
            {
                Log.WriteLineLoc($"[GravityMod] Activate World OnBlockChanged");
                World.OnBlockChanged.Add(GravityService.HandleBlockChange);
            };
        }

        public Task ShutdownAsync()
        {
            World.OnBlockChanged.Remove(GravityService.HandleBlockChange);

            return Task.CompletedTask;
        }

        public object GetEditObject() => this.config.Config;

        public void OnEditObjectChanged(object o, string param)
        {
            this.SaveConfig();
        }
    }
}
