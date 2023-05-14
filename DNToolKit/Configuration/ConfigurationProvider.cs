using DNToolKit.Configuration.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DNToolKit.Configuration
{
    /// <summary>
    /// The provider for configurations of type <see cref="Config"/>.
    /// </summary>
    public static class ConfigurationProvider
    {
        /// <summary>
        /// Load a configuration from <paramref name="configPath"/>.
        /// </summary>
        /// <typeparam name="TConfig">The type of configuration to parse. Has to be or derive from <see cref="Config"/>.</typeparam>
        /// <param name="configPath">The path to load the configuration from.</param>
        /// <returns>The loaded <typeparamref name="TConfig"/> or its empty implementation.</returns>
        public static TConfig LoadConfig<TConfig>(string? configPath = null)
            where TConfig : Config, new()
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return new TConfig() { ConfigPath = configPath };

            var text = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<TConfig>(text) ?? new TConfig();
            config.ConfigPath = configPath;
            return config;
        }

        /// <summary>
        /// Save the configuration to file, if <see cref="Config.ConfigPath"/> is set.
        /// </summary>
        /// <param name="config">The config to save.</param>
        /// <typeparam name="TConfig">The type of configuration to parse. Has to be or derive from <see cref="Config"/>.</typeparam>
        public static void SaveConfig<TConfig>(TConfig config)
            where TConfig : Config
        {
            if (config.ConfigPath is null) return;
            File.WriteAllText(config.ConfigPath, JsonConvert.SerializeObject(
                config, Formatting.Indented, new StringEnumConverter()));
        }
    }
}
