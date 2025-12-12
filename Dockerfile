# ---------------------------
# BASE IMAGE FOR BUILD
# ---------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy everything
COPY . .

# NuGet restore with retry + no parallel (avoids 503 rate limit errors)
RUN dotnet restore \
    --disable-parallel \
    --ignore-failed-sources \
    || (echo "Retrying restore after 5 sec..." && sleep 5 && dotnet restore --disable-parallel)

# Build + publish
RUN dotnet publish "CAT.AID.Web.csproj" -c Release -o /out --no-restore


# ---------------------------
# FINAL RUNTIME IMAGE
# ---------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app
COPY --from=build /out .

# Expose port (Render uses PORT env)
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
