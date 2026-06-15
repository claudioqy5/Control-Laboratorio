FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ControlLaboratorio.API.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/playwright/dotnet:v1.44.0-jammy AS base
RUN apt-get update && apt-get install -y tzdata
WORKDIR /app
COPY --from=build /app .
# El backend correrÃ¡ internamente en el puerto 80 (HTTP) dentro del contenedor
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "ControlLaboratorio.API.dll"]

