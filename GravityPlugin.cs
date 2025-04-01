using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Serialization;
using Eco.Core.Utils;
using Eco.Server;
using Eco.Shared.Math;
using Eco.Shared.Utils;
using Eco.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using GuidConverter = Eco.Core.Serialization.GuidConverter;
using Integrity = (decimal overhang, decimal resistance);

namespace Gravity;

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

    private const string SavePath = "Configs/Mods/Gravity/Internal";
    private const string ChangedColorPositionsPath = "Configs/Mods/Gravity/Internal/ChangedColorPositions.json";
    private const string WorldIntegritiesPath = "Configs/Mods/Gravity/Internal/WorldIntegrities.json";

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
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Converters =
            {
                new StringEnumConverter(),
                new JavaScriptDateTimeConverter(),
                new GuidConverter(),
                new LocStringConverter(),
                new Vector3IKeyDictionaryConverter<ByteColor>(),
                new Vector3IKeyDictionaryConverter<Integrity>(),
                new IntegrityConverter()
            },
        };

        if (File.Exists(ChangedColorPositionsPath))
        {
            GravityService.ChangedColorPositions = JsonConvert.DeserializeObject<Dictionary<WrappedWorldPosition3i, ByteColor>>(File.ReadAllText(ChangedColorPositionsPath), settings)!;
        }

        if (File.Exists(WorldIntegritiesPath))
        {
            GravityService.ChangedColorPositions = JsonConvert.DeserializeObject<Dictionary<WrappedWorldPosition3i, ByteColor>>(File.ReadAllText(WorldIntegritiesPath), settings)!;
        }

        PluginManager.Obj.InitComplete += () =>
        {
            Console.WriteLine(@"[GravityMod] Activate World OnBlockChanged");
            World.OnBlockChanged.Add(GravityService.HandleBlockChange);
        };
    }

    public async Task ShutdownAsync()
    {
        World.OnBlockChanged.Remove(GravityService.HandleBlockChange);

        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }

        await File.WriteAllTextAsync(ChangedColorPositionsPath, SerializationUtils.SerializeJson(GravityService.ChangedColorPositions));
        await File.WriteAllTextAsync(WorldIntegritiesPath, SerializationUtils.SerializeJson(GravityService.WorldIntegrities));
    }

    public object GetEditObject() => this.config.Config;

    public void OnEditObjectChanged(object o, string param)
    {
        this.SaveConfig();
    }
}

public class Vector3IKeyDictionaryConverter<TValue> : JsonConverter<Dictionary<WrappedWorldPosition3i, TValue>>
{
    public override Dictionary<WrappedWorldPosition3i, TValue> ReadJson(JsonReader reader, Type objectType, Dictionary<WrappedWorldPosition3i, TValue>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var tempDict = serializer.Deserialize<Dictionary<string, TValue>>(reader);
        var result = new Dictionary<WrappedWorldPosition3i, TValue>();

        if (tempDict == null) return result;

        foreach (var kvp in tempDict)
        {
            // Convert the string key "(x, y, z)" to WrappedWorldPosition3i
            var str = kvp.Key.Trim('(', ')');
            var parts = str.Split(',');

            if (parts.Length == 3
                && int.TryParse(parts[0].Trim(), out var x)
                && int.TryParse(parts[1].Trim(), out var y)
                && int.TryParse(parts[2].Trim(), out var z))
            {
                WrappedWorldPosition3i.TryCreate(new Vector3i(x, y, z), out var vec);

                // Deserialize TValue using the serializer
                var value = serializer.Deserialize<TValue>(new JTokenReader(JToken.FromObject(kvp.Value!)));
                result[vec] = value!;
            }
            else
            {
                throw new JsonSerializationException($"Format de clé invalide pour un WrappedWorldPosition3i : '{kvp.Key}'");
            }
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, Dictionary<WrappedWorldPosition3i, TValue>? value, JsonSerializer serializer)
    {
        if (value == null) return;

        var tempDict = value.ToDictionary(
            kvp => $"({kvp.Key.X}, {kvp.Key.Y}, {kvp.Key.Z})",
            kvp =>
            {
                // Serialize TValue using the serializer
                var token = JToken.FromObject(kvp.Value!, serializer);
                return token;
            });

        serializer.Serialize(writer, tempDict);
    }
}

public class IntegrityConverter : JsonConverter<Integrity>
{
    public override Integrity ReadJson(JsonReader reader, Type objectType, Integrity existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var temp = serializer.Deserialize<Dictionary<string, decimal>>(reader);
        if (temp == null || !temp.ContainsKey("o") || !temp.ContainsKey("r"))
        {
            throw new JsonSerializationException("Invalid JSON format for Integrity");
        }

        return (temp["o"], temp["r"]);
    }

    public override void WriteJson(JsonWriter writer, Integrity value, JsonSerializer serializer)
    {
        var temp = new Dictionary<string, decimal>
        {
            { "o", value.overhang },
            { "r", value.resistance }
        };

        serializer.Serialize(writer, temp);
    }
}
