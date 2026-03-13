# Estágio de Build: Usa o SDK 9.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Recebe a versão do Jenkins (padrão 1.0.0.0 se não enviada)
ARG VERSION=1.0.0.0

# Copia os arquivos para o container
COPY . .

# Ajuste: Adicionado o parâmetro de versão e o --no-self-contained no final
# Isso garante que apenas a DLL do plugin e o manifesto sejam publicados
RUN dotnet publish JFolderCollection/JFolderCollection.csproj \
    -c Release \
    -o /app/publish \
    /p:Version=${VERSION} \
    --no-self-contained \
    /p:CopyLocalLockFileAssemblies=false

# Copia o manifest.json para a pasta de publicação antes da extração final
# Usamos o caminho relativo conforme sua nova estrutura na raiz
COPY manifest.json /app/publish/

# Estágio de extração: Gera uma imagem leve apenas com os binários
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/publish .