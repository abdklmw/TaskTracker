# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install Node.js so we can run gulp
RUN apt-get update && apt-get install -y curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copy project files first (better layer caching)
COPY TaskTracker.csproj .
RUN dotnet restore

# Copy the rest of the source
COPY . .

# Install npm packages and run gulp to generate minified assets
RUN npm install
RUN npx gulp

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-restore

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directories the app expects
RUN mkdir -p /app/data/log /app/data/keys

# Copy the published output
COPY --from=build /app/publish .

# Environment defaults
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TaskTracker.dll"]