﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;

using RBot.Utils;

namespace RBot.Plugins
{
    public delegate void PluginLoadDelegate(PluginContainer container);

    public class PluginManager
    {
        private static Dictionary<RPlugin, PluginContainer> _plugins = new Dictionary<RPlugin, PluginContainer>();

        /// <summary>
        /// Gets a list of currently loaded plugins' containers.
        /// </summary>
        public static List<PluginContainer> Containers => _plugins.Values.ToList();

        /// <summary>
        /// Fires when a plugin is loaded.
        /// </summary>
        public static event PluginLoadDelegate PluginLoaded;
        /// <summary>
        /// Fires when a plugin is unloaded.
        /// </summary>
        public static event PluginLoadDelegate PluginUnloaded;

        /// <summary>
        /// Loads all the plugins in the plugins folder. This is called when RBot loads.
        /// </summary>
        public static void Init()
        {
            if (!Directory.Exists("plugins/options"))
                Directory.CreateDirectory("plugins/options");
            Directory.GetFiles("plugins").Where(f => f.EndsWith(".dll")).ForEach(f => Load(f));
        }

        /// <summary>
        /// Loads the plugin at the given path.
        /// </summary>
        /// <param name="path">The path to the plugin assembly.</param>
        /// <returns>Whether the plugin was loaded successfully or not.</returns>
        public static Exception Load(string path)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(path);
                TypeInfo type = asm.DefinedTypes.FirstOrDefault(t => t.IsSubclassOf(typeof(RPlugin)));
                if (type == null)
                    return new Exception("Plugin class not found.");
                else
                {
                    RPlugin plugin = Activator.CreateInstance(type) as RPlugin;
                    if (plugin == null)
                        return new Exception("Failed to create plugin instance.");
                    else
                    {
                        PluginContainer container = new PluginContainer(plugin);
                        _plugins.Add(plugin, container);
                        plugin.Load();
                        container.Options.SetDefaults();
                        container.Options.Load();
                        PluginLoaded?.Invoke(container);
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>
        /// Unloads the given plugin.
        /// </summary>
        /// <param name="plugin">The plugin to unload.</param>
        public static void Unload(RPlugin plugin)
        {
            PluginUnloaded?.Invoke(_plugins[plugin]);
            plugin.Unload();
            _plugins.Remove(plugin);
        }

        /// <summary>
        /// Gets the container for the given plugin.
        /// </summary>
        /// <param name="plugin">The plugin to get the container for.</param>
        /// <returns>The plugin's container.</returns>
        public static PluginContainer GetContainer(RPlugin plugin)
        {
            return _plugins.TryGetValue(plugin, out PluginContainer container) ? container : null;
        }

        /// <summary>
        /// Gets the container for the plugin with the given type.
        /// </summary>
        /// <typeparam name="T">The type of the plugin to get the container for.</typeparam>
        /// <returns>The container for the plugin with the given type.</returns>
        public static PluginContainer GetContainer<T>() where T : RPlugin
        {
            return _plugins.FirstOrDefault((KeyValuePair<RPlugin, PluginContainer> kvp) => kvp.Key is T).Value;
        }
    }
}
