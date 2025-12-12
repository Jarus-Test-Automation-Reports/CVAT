FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Publish the project
RUN dotnet publish "CAT.AID.Web/CAT.AID.Web.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
