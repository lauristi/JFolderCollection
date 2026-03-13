# Estágio de Build: Usa o SDK 9.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os arquivos para o container
COPY . .

# Ajuste: Apontamos para o .csproj em vez do .sln para evitar o warning NETSDK1194
RUN dotnet publish JFolderCollection/JFolderCollection.csproj -c Release -o /app/publish

# Copiado manifest.json para o diretório de publicação
COPY manifest.json /app/publish/

# Estágio de extração
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/publish .