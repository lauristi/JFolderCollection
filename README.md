# 📁 JFolderCollection

> Plugin para o **Jellyfin 10.11+** que cria coleções automaticamente a partir da estrutura de pastas do seu servidor de mídia.

![Jellyfin](https://img.shields.io/badge/Jellyfin-10.11%2B-00A4DC?style=flat-square&logo=jellyfin)
![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Build](https://img.shields.io/badge/Build-Jenkins-D24939?style=flat-square&logo=jenkins)

---

## 📖 Sobre o Plugin

O **JFolderCollection** resolve um problema comum em servidores Jellyfin com grandes bibliotecas de filmes organizadas por pastas: criar e manter coleções manualmente é tedioso e propenso a erros.

Com este plugin, você define o diretório raiz onde seus filmes estão organizados e ele se encarrega de:

- Varrer a estrutura de subpastas
- Criar uma coleção (BoxSet) no Jellyfin para cada subpasta encontrada
- Vincular os filmes de cada pasta à sua coleção correspondente
- Aplicar posters automaticamente às coleções
- Detectar filmes duplicados na sua biblioteca

---

## 🗂️ Estrutura de Pastas Esperada

O plugin espera que sua biblioteca esteja organizada da seguinte forma:

```
/mnt/filmes/
├── Coleção Ação/
│   ├── Mad Max Fury Road/
│   │   └── mad.max.fury.road.mkv
│   └── John Wick/
│       └── john.wick.mkv
├── Coleção Ficção Científica/
│   ├── Interstellar/
│   │   └── interstellar.mkv
│   └── Dune/
│       └── dune.mkv
└── ...
```

Cada **subpasta de primeiro nível** vira uma coleção. Cada **subpasta dentro dela** é tratada como um filme.

---

## ✨ Funcionalidades

| Funcionalidade | Descrição |
|---|---|
| **Criação de Coleções** | Cria BoxSets no Jellyfin baseado na estrutura de pastas |
| **Substituição de Prefixo** | Renomeia coleções substituindo um prefixo por outro durante a criação |
| **Posters Automáticos** | Copia arquivos `.png` da pasta de posters para cada coleção |
| **Modo Incremental** | Cria apenas coleções novas, pulando as que já existem |
| **Limpeza Total** | Apaga todas as coleções antes de recriar (reset completo) |
| **Listar Pastas** | Lista as subpastas do diretório configurado |
| **Buscar Duplicados** | Detecta arquivos de vídeo com o mesmo nome em locais diferentes |

---

## ⚙️ Instalação

### Requisitos

- Jellyfin **10.11.0** ou superior
- .NET **9.0** (já incluso no container Jellyfin)

### Instalação Manual

1. Baixe a última versão na aba [Releases](../../releases)
2. Copie os arquivos para a pasta de plugins do Jellyfin:
   ```
   /config/plugins/JFolderCollection/
   ```
3. Reinicie o servidor Jellyfin
4. Acesse **Painel → Plugins → JFolderCollection → Configurações**

### Via Repositório (recomendado)

1. Acesse **Painel → Plugins → Repositórios**
2. Clique em **+** e adicione a URL:
   ```
   https://raw.githubusercontent.com/lauristi/JFolderCollection/master/manifest.json
   ```
3. Acesse **Catálogo**, localize o plugin e instale
4. Reinicie o servidor

---

## 🔧 Configuração

Após a instalação, acesse **Painel → Plugins → JFolderCollection → Configurações**.

### Diretórios

| Campo | Descrição |
|---|---|
| **Caminho da Coleção de Filmes** | Diretório raiz onde as pastas de coleções estão organizadas |
| **Caminho dos Posters** | Pasta contendo arquivos `.png` com o mesmo nome de cada pasta de coleção |

### Regras de Processamento

| Campo | Descrição |
|---|---|
| **Buscar por (Prefixo)** | Texto a ser localizado no nome da pasta |
| **Substituir por** | Texto pelo qual o prefixo será substituído no nome da coleção |
| **Criar apenas novas coleções** | Se marcado, pula pastas que já possuem coleção correspondente |
| **Apagar todas antes de criar** | ⚠️ Remove **todas** as coleções existentes antes de iniciar |

### Exemplo de Substituição de Prefixo

Se suas pastas se chamam `COL_Ação`, `COL_Drama`, etc. e você quer que as coleções apareçam como `Coleção Ação`, `Coleção Drama`:

- **Buscar por:** `COL_`
- **Substituir por:** `Coleção `

### Posters

Os arquivos de poster devem estar na pasta configurada em **Caminho dos Posters**, com o mesmo nome da pasta da coleção:

```
/mnt/posters/
├── Coleção Ação.png
├── Coleção Ficção Científica.png
└── ...
```

---

## 🛠️ Build e Deploy

O projeto utiliza **Jenkins** e **Docker** para CI/CD automatizado.

### Pré-requisitos

- Jenkins com acesso ao Docker do host
- Docker instalado no servidor
- Container Jellyfin rodando com volume de plugins montado

### Pipeline Jenkins

O `Jenkinsfile` na raiz do projeto define três estágios:

```
01 - Checkout      → Clona o repositório
02 - Build Plugin  → Compila via Docker SDK .NET 9
03 - Deploy        → Copia DLL para o container Jellyfin e reinicia
```

### Variáveis a Configurar no Jenkinsfile

```groovy
JELLYFIN_CONTAINER = 'jellyfin'          // Nome do container Docker
INTERNAL_PLUGIN_PATH = '/config/plugins/JFolderCollection'  // Path interno no container
```

### Build Manual

```bash
# Compilar
dotnet publish JFolderCollection/JFolderCollection.csproj \
    -c Release \
    -o ./publish \
    --no-self-contained \
    /p:UseAppHost=false

# Copiar para o container
docker cp ./publish/. jellyfin:/config/plugins/JFolderCollection/
docker restart jellyfin
```

---

## 🏗️ Arquitetura

```
JFolderCollection/
├── Plugin.cs                        # Ponto de entrada — singleton do plugin
├── Controllers/
│   └── MainController.cs            # Endpoints REST da API
├── Configuration/
│   ├── PluginConfiguration.cs       # Modelo de configuração persistida em XML
│   └── configPage.html              # Interface administrativa (Embedded Resource)
├── Models/
│   └── CreateCollectionRequest.cs   # DTO para criação de coleções
└── Images/
    ├── plugin-thumbnail.png
    └── plugin-icon.png
```

### Endpoints da API

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/Plugin/Folder/Subfolders` | Lista subpastas de um diretório |
| `GET` | `/Plugin/Folder/DuplicateMovies` | Detecta filmes duplicados |
| `POST` | `/Plugin/Folder/CreateCollections` | Executa a criação de coleções |

---

## 🔍 Ferramentas de Diagnóstico

Na página de configurações do plugin, a seção **Ferramentas** oferece:

- **Listar Pastas** — verifica se o caminho configurado está acessível e lista as subpastas encontradas
- **Buscar Duplicados** — escaneia recursivamente o diretório em busca de arquivos de vídeo com nomes duplicados, exibindo o número de cópias e os caminhos de cada uma

---

## 📋 Compatibilidade

| Versão Jellyfin | Compatível |
|---|---|
| 10.11.x | ✅ |
| 10.10.x | ⚠️ Não testado |
| 10.9.x e anteriores | ❌ |

> **Nota:** O Jellyfin 10.11 migrou o banco de dados interno para EF Core. Este plugin foi desenvolvido e testado exclusivamente contra a versão 10.11+.

---

## 🤝 Contribuindo

1. Faça um fork do repositório
2. Crie uma branch para sua feature: `git checkout -b feature/minha-feature`
3. Commit suas mudanças: `git commit -m 'feat: minha nova feature'`
4. Push para a branch: `git push origin feature/minha-feature`
5. Abra um Pull Request

---

## 👤 Autor

**Lauris TI**

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
