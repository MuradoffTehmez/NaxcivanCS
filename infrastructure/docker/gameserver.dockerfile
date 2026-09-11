# PRD 99, 100, 101 - NaxcivanCS dedicated server konteyneri (Linux headless).
# Qurulus:  docker build -f infrastructure/docker/gameserver.dockerfile -t naxcivancs-server:0.1.0 .
# Isledilme: docker run --rm -p 27015:27015/udp naxcivancs-server:0.1.0

ARG GODOT_VERSION=4.7.2

# ---------- 1) Godot headless export ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG GODOT_VERSION
ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl unzip \
 && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL -o /tmp/godot.zip \
      "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64.zip" \
 && unzip -q /tmp/godot.zip -d /opt \
 && mv "/opt/Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64" /opt/godot \
 && ln -s "/opt/godot/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64" /usr/local/bin/godot \
 && rm /tmp/godot.zip

RUN curl -fsSL -o /tmp/templates.tpz \
      "https://github.com/godotengine/godot/releases/download/${GODOT_VERSION}-stable/Godot_v${GODOT_VERSION}-stable_mono_export_templates.tpz" \
 && mkdir -p "/root/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono" \
 && unzip -q /tmp/templates.tpz -d /tmp/tpl \
 && mv /tmp/tpl/templates/* "/root/.local/share/godot/export_templates/${GODOT_VERSION}.stable.mono/" \
 && rm -rf /tmp/templates.tpz /tmp/tpl

WORKDIR /src
COPY . .

RUN godot --headless --path server --import \
 && godot --headless --path server --export-release "Linux Server" /out/NaxcivanCS.Server

# ---------- 2) Runtime ----------
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS runtime

RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/* \
 && useradd --create-home --uid 10001 naxcivan

WORKDIR /app
COPY --from=build /out/ /app/
COPY --chown=naxcivan:naxcivan config/ /app/config/

USER naxcivan

# PRD 41, 42 - ENet/UDP, 64 tick
EXPOSE 27015/udp

ENTRYPOINT ["/app/NaxcivanCS.Server", "--headless", "--"]
CMD ["--port", "27015", "--map", "NC_Qala"]
