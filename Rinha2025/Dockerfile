FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

# copia todos os csproj e restaura dependendencias
COPY *.csproj ./
RUN dotnet restore

# build da aplicação
COPY . ./
RUN dotnet publish -c Release -o out

# build da imagem
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
RUN mkdir -p Data && chmod 777 Data
COPY --from=build /app/out .
ENTRYPOINT [ "dotnet","Rinha2025.dll" ]
