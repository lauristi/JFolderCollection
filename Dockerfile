# Estágio de Build: Usa o SDK 9.0 para compilar
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os arquivos da solução e do projeto
COPY . .

# Restaura e publica a DLL
RUN dotnet publish JFolderCollection.sln -c Release -o /app/publish

# Estágio de extração: imagem leve apenas para segurar os arquivos
FROM alpine:latest
WORKDIR /app
COPY --from=build /app/publish .