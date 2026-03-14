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
    /// Classe principal do plugin JFolderCollection.
    /// Gerencia a integração com o servidor Jellyfin e a exposição da interface administrativa.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        #region Construtor e Instância Static

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [JFolderCollection] 🎯 Plugin construído com sucesso.");
        }

        /// <summary>
        /// Instância singleton do plugin — usada pelos controllers para acessar
        /// Plugin.Instance.Configuration sem precisar de injeção de dependência.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        #endregion

        #region Metadados

        public override string Name => "JFolderCollection";

        /// <remarks>
        /// GUID fixo — nunca alterar após o primeiro deploy.
        /// Alterar o GUID faz o Jellyfin tratar o plugin como um novo,
        /// perdendo todas as configurações salvas anteriormente.
        /// </remarks>
        public override Guid Id => Guid.Parse("c7b8d1b3-41d9-4a19-b04e-f43534455342");

        public override string Description => "Plugin para gerenciar e listar pastas de mídia no Jellyfin.";

        #endregion

        #region Páginas de Configuração (IHasWebPages)

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    // Caminho do recurso embutido: Namespace + estrutura de pastas com pontos.
                    // Bate com: <EmbeddedResource Include="Configuration\configPage.html" />
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }

        #endregion

        #region Imagem do Plugin

        // CORREÇÃO: O Jellyfin 10.9+ serve a thumbnail via stream do recurso embutido
        // lido diretamente pelo servidor através da propriedade abaixo.
        // O método GetThumbImage() avulso não é chamado pelo servidor — era uma
        // convenção de versões antigas que não faz mais parte da interface pública.
        //
        // O servidor localiza a imagem procurando por um recurso embutido cujo nome
        // termine com "thumb.png" OU pelo override de AssemblyFilePath abaixo.
        // A forma mais simples e compatível com 10.9+ é nomear o arquivo como
        // "thumb.png" no projeto e declará-lo como EmbeddedResource.
        //
        // Se você preferir manter o nome "plugin-thumbnail.png", implemente
        // IHasPluginImage (disponível em Jellyfin.Controller 10.9+):
        //
        //   public string ThumbImagePath => 
        //       $"{GetType().Namespace}.Images.plugin-thumbnail.png";
        //
        // Por compatibilidade e simplicidade, mantemos o override do AssemblyFilePath
        // e deixamos o Jellyfin resolver via convenção de nome de recurso.

        /// <summary>
        /// Retorna o stream da imagem thumbnail do plugin.
        /// Chamado pelo servidor ao renderizar a página de plugins.
        /// </summary>
        /// <remarks>
        /// IMPORTANTE: O caminho do recurso embutido segue a regra:
        /// RootNamespace + caminho de pastas com separadores trocados por pontos.
        /// 
        /// Exemplo: Images\plugin-thumbnail.png
        ///       => JFolderCollection.Images.plugin-thumbnail.png
        ///
        /// Para verificar os nomes exatos dos recursos embutidos no assembly,
        /// use: Assembly.GetManifestResourceNames() nos logs de inicialização.
        /// </remarks>
        public Stream? GetThumbImage()
        {
            var type = GetType();
            // CORREÇÃO: caminho inclui a subpasta "Images" como parte do nome do recurso.
            // <EmbeddedResource Include="Images\plugin-thumbnail.png" />
            //                                ^^^^^^ vira ponto no nome do recurso
            return type.Assembly.GetManifestResourceStream(
                $"{type.Namespace}.Images.plugin-thumbnail.png");
        }

        #endregion
    }
}
