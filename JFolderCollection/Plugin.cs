namespace JFolderCollection
{
    using JFolderCollection.Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Model.Plugins;
    using MediaBrowser.Model.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Classe principal do plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Inicializa uma nova instância do plugin.
        /// </summary>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            // 🚨 DEBUG CRÍTICO
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Jellyfin.Plugin.Template] 🎯 PLUGIN CONSTRUIDO!");
        }

        /// <inheritdoc />
        public override string Name => "Template";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("c7b8d1b3-41d9-4a19-b04e-f43534455342");

        /// <summary>
        /// Gets instância atual do plugin.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Description => "Plugin para gerenciar e listar pastas de mídia no Jellyfin.";

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Jellyfin.Plugin.Template] 📄 GetPages() chamado!");

            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }
    }
}