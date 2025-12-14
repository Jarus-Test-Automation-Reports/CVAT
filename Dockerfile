# ============================
#   BUILD STAGE
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only csproj first (better caching)
COPY CAT.AID.Web.csproj ./

# Restore dependencies
RUN dotnet restore --disable-parallel

# Copy the rest of the project
COPY . .

# Publish
RUN dotnet publish -c Release -o /app/publish


# ============================
#   RUNTIME STAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Ensure Linux has required global fonts (QuestPDF PDF rendering)
RUN apt-get update && apt-get install -y \
    fontconfig \
    fonts-dejavu \
    && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish ./

# Ensure wwwroot, images, data files are included
# (sometimes Docker ignores empty folders)
RUN mkdir -p /app/wwwroot/Images
RUN mkdir -p /app/wwwroot/data

# Expose Render/Fly.io compatible port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
