# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY KindredLabs.Core/KindredLabs.Core.csproj KindredLabs.Core/
COPY KindredLabs.Web/KindredLabs.Web.csproj KindredLabs.Web/
RUN dotnet restore KindredLabs.Web/KindredLabs.Web.csproj

# Copy everything and build
COPY . .
RUN dotnet publish KindredLabs.Web/KindredLabs.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

# Cloud Run requires listening on PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

ENTRYPOINT ["dotnet", "KindredLabs.Web.dll"]