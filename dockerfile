########################################################
# Build final docker image                             #
########################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

ARG APP_NAME
ARG version
ARG ENVIRONMENT

ENV NEW_RELIC_APP_NAME $APP_NAME.$ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT $ENVIRONMENT

WORKDIR /app
EXPOSE 80

# Image for builder to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
EXPOSE 80
COPY . ./
RUN dotnet publish ./src/QuantityTakeoffOrchestratorService/QuantityTakeoffOrchestratorService.csproj -c Release -o /out

# Copy resources from publish and set the entrypoint
FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENV BUILD_NUMBER="${version}"

# copy the certs
COPY /mep.platform.pfx mep.platform.pfx
COPY /prod.mep.platform.pfx prod.mep.platform.pfx

# Enable the agent
ENV CORECLR_ENABLE_PROFILING=0 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/app/newrelic \
CORECLR_PROFILER_PATH=/app/newrelic/libNewRelicProfiler.so

# Install Curl
RUN apt-get update && apt-get install -y curl

ENV ASPNETCORE_HTTP_PORTS=80

# Start the app
ENTRYPOINT ["dotnet", "QuantityTakeoffOrchestratorService.dll"]
