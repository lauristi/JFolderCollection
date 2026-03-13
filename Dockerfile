# Estágio de Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG VERSION=1.0.0.0

# 1. AJUSTE: Copia apenas o arquivo de projeto primeiro para otimizar o cache
COPY JFolderCollection/JFolderCollection.csproj JFolderCollection/
RUN dotnet restore JFolderCollection/JFolderCollection.csproj

# 2. Copia o restante (certifique-se de ter o .dockerignore que mencionei antes)
COPY . .

# 3. AJUSTE NO PUBLISH: Adicionamos flags para impedir a criação de lixo XML e PDB
RUN dotnet publish JFolderCollection/JFolderCollection.csproj \
    -c Release \
    -o /app/publish \
    /p:Version=${VERSION} \
    --no-self-contained \
    /p:CopyLocalLockFileAssemblies=false \
    /p:DebugType=none \
    /p:DebugSymbols=false \
    /p:GenerateDocumentationFile=false

# Copia o manifest.json
COPY manifest.json /app/publish/

# Estágio de extração
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/publish .