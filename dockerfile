FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
ENV DOTNET_EnableDiagnostics=0

COPY --from=build-env ./out ./app

ENTRYPOINT ["app/MasterServer"]