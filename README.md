# TorBox Tinfoil Server (C# Edition)

Servidor local, escrito en C#/.NET, para conectar tu cuenta de [TorBox](https://torbox.app) a **Tinfoil** o **Cyberfoil** en tu Nintendo Switch. Actúa como puente entre tu librería de TorBox y tu Switch, permitiendo instalar directamente tus archivos `.nsp`, `.nsz`, `.xci` y `.xcz`.

Este proyecto nace como alternativa al servidor oficial [`torbox-tinfoil-server`](https://github.com/TorBox-App/torbox-tinfoil-server) (Python), después de encontrar un bug de manejo de HTTP Range Requests (`BufferDataRange:462: HTTP range read failed`) que afectaba las instalaciones vía Cyberfoil.

## Características

- Compatible con **Tinfoil** y **Cyberfoil**
- Soporte correcto de **HTTP Range Requests** (streaming eficiente, sin descargar el archivo completo en el proxy)
- Autenticación Basic Auth configurable
- Configuración vía `appsettings.json` o variables de entorno
- Corre standalone (.exe) o en Docker
- Manejo seguro de torrents en proceso de descarga (evita crashes cuando `files` aún no está disponible)

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (para compilar) — no necesario si usas el `.exe` ya publicado o Docker
- Una cuenta de [TorBox](https://torbox.app) con tu API key (Settings → API)
- Docker y Docker Compose (opcional, si prefieres correrlo en contenedor)

## Opción 1: Ejecutable standalone (Windows)

1. Descarga el `.exe` desde [Releases](../../releases)
2. Edita `appsettings.json` en la misma carpeta:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*",
     "TorBox": {
       "ApiKey": "TU_API_KEY_AQUI",
       "AuthUser": "admin",
       "AuthPass": "adminadmin",
       "Port": 8000
     }
   }
   ```
3. Ejecuta `TorBoxTinfoilServer.exe` y deja la ventana abierta.

## Opción 2: Docker

1. Clona el repo:
   ```bash
   git clone https://github.com/TU_USUARIO/TorBoxTinfoilServer.git
   cd TorBoxTinfoilServer
   ```
2. Copia la plantilla de variables de entorno y edítala con tu API key real:
   ```bash
   cp .env.example .env
   ```
   ```env
   TORBOX_API_KEY=tu_api_key_real_aqui
   AUTH_USER=admin
   AUTH_PASS=adminadmin
   ```
3. Levanta el contenedor:
   ```bash
   docker compose up -d --build
   ```
4. Verifica que esté corriendo:
   ```bash
   docker compose logs -f
   ```

Cada vez que modifiques el código fuente, vuelve a correr `docker compose up -d --build` para reconstruir la imagen.

## Configurar Tinfoil / Cyberfoil en el Switch

Al agregar un servidor remoto:

| Campo        | Valor                                  |
|--------------|-----------------------------------------|
| Protocolo    | `http`                                  |
| Host         | IP local de tu PC/servidor (ej. `192.168.1.X`) |
| Puerto       | `8000`                                  |
| Path         | (vacío)                                 |
| Usuario      | El que pusiste en `AuthUser` / `AUTH_USER` |
| Contraseña   | El que pusiste en `AuthPass` / `AUTH_PASS` |

> Usa `http`, no `https` — no es necesario cifrado en tu red local, y evita complicaciones de certificados.

## Compilar y publicar tú mismo

**Ejecutable standalone (Windows):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./release
```

**Build normal (dispara la generación de `appsettings.example.json`):**
```bash
dotnet build -c Release
```

## Estructura del proyecto

```
TorBoxTinfoilServer/
├── Program.cs                   # Lógica principal del servidor
├── TorBoxTinfoilServer.csproj
├── appsettings.json              # Config real (ignorado por git)
├── appsettings.example.json      # Plantilla (se genera en cada build)
├── generate-example.ps1          # Script que genera el .example
├── Dockerfile
├── docker-compose.yml
├── .env.example                  # Plantilla de variables de entorno para Docker
├── .gitignore
└── .dockerignore
```

## Notas técnicas

- El endpoint `/` expone el índice de archivos instalables (`.nsp/.nsz/.xci/.xcz`) de tu cuenta TorBox.
- El endpoint `/torrents/{torrentId}/{fileId}` hace de proxy hacia la CDN de TorBox, reenviando correctamente el header `Range` y propagando `206 Partial Content` cuando corresponde — esto es lo que soluciona el error de instalación en Cyberfoil.
- Torrents que aún están descargándose (con `files: null`) se ignoran de forma segura en vez de causar un error 500.

## Advertencia

Este proyecto **no provee juegos ni fomenta la piratería**. Es simplemente un puente técnico para transferir a tu Switch archivos que ya posees legalmente dentro de tu cuenta de TorBox — de la misma forma que el servidor oficial de TorBox lo hacía antes de deshabilitar su versión hosteada.

## Créditos

Inspirado en [`TorBox-App/torbox-tinfoil-server`](https://github.com/TorBox-App/torbox-tinfoil-server), reescrito en C#/.NET para resolver un bug de manejo de HTTP Range Requests con Cyberfoil.
