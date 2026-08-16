# syntax=docker/dockerfile:1

# The API as a container.
#
# Multi-stage: the SDK is about 800MB and is needed only to compile. The image
# that ships carries the runtime and the build output, and nothing that could
# compile anything — which is both smaller and a smaller attack surface. A
# shell popped inside a runtime-only image has no compiler to build with.
#
#   docker build -t lubnan-api -f server/Dockerfile server
#   docker compose up -d --build

# ── Build ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Project files first, restore, then the rest of the source.
#
# The order is the whole trick. Docker caches a layer until one of its inputs
# changes, and dependencies change far less often than code does — so restoring
# before copying the source means editing a handler does not re-download the
# NuGet graph. Copying everything up front would invalidate the restore on
# every keystroke.
# .editorconfig is not optional here. EnforceCodeStyleInBuild is on and CI turns
# warnings into errors, so the analyser configuration is a build input: without
# it the image fails on rules the host suppresses, and the error arrives from a
# file nobody edited.
COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Lubnan.Domain/*.csproj          src/Lubnan.Domain/
COPY src/Lubnan.Application/*.csproj     src/Lubnan.Application/
COPY src/Lubnan.Infrastructure/*.csproj  src/Lubnan.Infrastructure/
COPY src/Lubnan.Api/*.csproj             src/Lubnan.Api/

RUN dotnet restore src/Lubnan.Api/Lubnan.Api.csproj

COPY src/ src/

# ContinuousIntegrationBuild turns warnings into errors and makes the output
# deterministic: two builds of one commit produce identical bytes.
RUN dotnet publish src/Lubnan.Api/Lubnan.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app \
      -p:ContinuousIntegrationBuild=true

# ── Runtime ─────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Alpine's ICU is not installed by default and .NET falls back to invariant
# globalization, which silently breaks culture-aware comparison and formatting.
# This application serves Arabic and French; it needs the real thing.
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Non-root, and it owns nothing it does not need to write.
#
# The default is root, and a container process running as root that escapes its
# namespace is root on the host. There is no reason for a web API to be able to
# write to its own binaries.
RUN addgroup -S lubnan && adduser -S lubnan -G lubnan
COPY --from=build --chown=root:root /app .
USER lubnan

# Above 1024, because binding a privileged port is one of the few things a
# non-root process cannot do.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Liveness only. Readiness depends on Postgres, and a container that reports
# itself unhealthy because a database is briefly unreachable gets restarted by
# the orchestrator — turning a thirty-second blip into a restart loop across
# every replica. Readiness belongs to the orchestrator's own probe.
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Lubnan.Api.dll"]
