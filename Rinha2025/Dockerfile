FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /app

# copia todos os csproj e restaura dependendencias
COPY *.csproj ./
RUN dotnet restore

# build da aplicação
COPY . ./
RUN dotnet publish -c Release -o out

# build da imagem
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet","Rinha2025.dll" ]
