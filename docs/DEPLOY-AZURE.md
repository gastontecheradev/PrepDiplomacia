# Deploy en Azure App Service

Guía paso a paso para publicar **Prep Diplomacia** en Azure.

> Esta guía asume Linux App Service .NET 8 con SQLite. El paso a Azure SQL se cubre al final.

---

## 1. Crear los recursos en Azure

### 1.1 App Service Plan + App Service

1. Entrar al [portal de Azure](https://portal.azure.com).
2. Crear un nuevo recurso → **App Service**.
3. Configurar:
   - **Resource Group:** `rg-prepdiplomacia`
   - **Name:** `prepdiplomacia` (URL final: `prepdiplomacia.azurewebsites.net`).
   - **Publish:** Code
   - **Runtime stack:** **.NET 8 (LTS)**
   - **OS:** Linux
   - **Region:** Brazil South (la más cercana a Uruguay) o East US 2.
   - **App Service Plan:** Plan **B1** (Basic) para empezar (≈ USD 13/mes); escalable a P1V3 cuando aumente el tráfico.
4. Crear el recurso.

### 1.2 Dominio personalizado (cuando esté disponible)

1. En el App Service → **Custom domains** → **Add custom domain**.
2. Ingresar `prepdiplomacia.uy`.
3. Configurar el registro **CNAME** o **A** en el panel del dominio según indique Azure.
4. Activar **Managed Certificate** (gratis) para HTTPS.

---

## 2. Configurar Application Settings (variables de entorno)

En el App Service → **Configuration → Application settings**, agregar (en .NET, las claves anidadas usan **doble guion bajo** `__` como separador):

| Clave | Valor |
|-------|-------|
| `DatabaseProvider` | `Sqlite` |
| `ConnectionStrings__Default` | `Data Source=/home/data/prepdiplomacia.db` |
| `Email__FromEmail` | `prepdiplomaciauy@gmail.com` |
| `Email__FromNombre` | `Prep Diplomacia` |
| `Email__DestinoFormularios` | `prepdiplomaciauy@gmail.com` |
| `Email__Smtp__Host` | `smtp.gmail.com` |
| `Email__Smtp__Port` | `587` |
| `Email__Smtp__UsarStartTls` | `true` |
| `Email__Smtp__Usuario` | `prepdiplomaciauy@gmail.com` |
| `Email__Smtp__Password` | *(App Password de Gmail)* |
| `Stripe__SecretKey` | `sk_live_...` |
| `Stripe__PublishableKey` | `pk_live_...` |
| `Stripe__WebhookSecret` | `whsec_...` |
| `Stripe__SiteUrl` | `https://prepdiplomacia.uy` |
| `Stripe__MonedaDefault` | `usd` |
| `Mailchimp__ApiKey` | *(API key)* |
| `Mailchimp__DataCenter` | *(ej. `us21`)* |
| `Mailchimp__AudienceId` | *(ID de la audiencia)* |
| `Mailchimp__DoubleOptIn` | `true` |
| `Admin__Email` | `prepdiplomaciauy@gmail.com` |
| `Admin__Password` | *(contraseña inicial fuerte)* |
| `WEBSITES_PORT` | `8080` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

> 💡 La SQLite va en `/home/data/prepdiplomacia.db` para que esté en el área persistente del App Service.

---

## 3. Generar la App Password de Gmail

1. <https://myaccount.google.com/security> → activar **Verificación en dos pasos**.
2. <https://myaccount.google.com/apppasswords> → generar una contraseña de aplicación llamada `Prep Diplomacia – Azure`.
3. Pegarla en `Email__Smtp__Password`.

---

## 4. Configurar el webhook de Stripe en producción

1. Stripe Dashboard → **Developers → Webhooks → Add endpoint**.
2. URL: `https://prepdiplomacia.uy/pago/webhook` (o `https://prepdiplomacia.azurewebsites.net/pago/webhook` mientras no esté el dominio).
3. Eventos a escuchar:
   - `checkout.session.completed`
   - `invoice.paid`
   - `invoice.payment_failed`
4. Copiar el **Signing secret** y pegarlo en `Stripe__WebhookSecret`.
5. **Reiniciar el App Service** para que tome la nueva configuración.

---

## 5. Publicar el código

### Opción A — desde Visual Studio (más rápida la primera vez)

1. Abrir `PrepDiplomacia.sln`.
2. Click derecho sobre `PrepDiplomacia.Web` → **Publish**.
3. Target: **Azure** → **Azure App Service (Linux)**.
4. Seleccionar la app `prepdiplomacia`.
5. Publish.

### Opción B — GitHub Actions (recomendada para producción)

Crear `.github/workflows/azure.yml`:

```yaml
name: Deploy a Azure App Service

on:
  push:
    branches: [ main ]
  workflow_dispatch:

env:
  AZURE_WEBAPP_NAME: prepdiplomacia
  DOTNET_VERSION: '8.x'
  PROJECT: src/PrepDiplomacia.Web/PrepDiplomacia.Web.csproj

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore ${{ env.PROJECT }}

      - name: Publish
        run: dotnet publish ${{ env.PROJECT }} -c Release -o ./publish

      - name: Deploy to Azure
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

En el portal de Azure → App Service → **Get publish profile** y copiar el contenido al secreto del repo `AZURE_WEBAPP_PUBLISH_PROFILE`.

---

## 6. Configurar el primer arranque

Cuando la app levanta, ejecuta automáticamente:

1. `MigrateAsync()` — crea/actualiza la base.
2. Seed de roles, usuario admin, planes y bloques de contenido.

Verificar que esté funcionando:

- `https://prepdiplomacia.azurewebsites.net/` — debería mostrar el home.
- `https://prepdiplomacia.azurewebsites.net/admin` — login con las credenciales del admin.

---

## 7. Persistencia de archivos subidos

Los archivos subidos al blog se guardan en `wwwroot/uploads/`. En App Service Linux, **`wwwroot` se borra en cada despliegue**, así que hay que mover los uploads a una carpeta persistente.

### Opción 1 — Carpeta `/home/data` (más simple)

1. Crear el directorio en Kudu (`https://prepdiplomacia.scm.azurewebsites.net/newui/fileManager`):
   - `/home/site/wwwroot/uploads` → mover a `/home/data/uploads`
2. Crear un symlink desde wwwroot/uploads → /home/data/uploads (en startup script).

### Opción 2 — Azure Blob Storage (recomendada largo plazo)

Reemplazar `FileStorageLocalService` por una nueva implementación de `IFileStorageService` que use Azure Blob. La interfaz ya está pensada para esto:

```csharp
public class FileStorageBlobService : IFileStorageService
{
    // Implementación con BlobServiceClient
}
```

Y registrar en `Program.cs`:

```csharp
builder.Services.AddScoped<IFileStorageService, FileStorageBlobService>();
```

---

## 8. Migrar a Azure SQL (cuando crezca)

1. Crear **Azure SQL Database** (DTU Basic ≈ USD 5/mes para empezar).
2. Habilitar el firewall para que el App Service pueda conectarse:
   - Server → **Networking** → habilitar «Allow Azure services and resources».
3. Cambiar las Application Settings:
   - `DatabaseProvider` → `SqlServer`
   - `ConnectionStrings__Default` → connection string de SQL.
4. Ejecutar la nueva migración o aplicarla automáticamente al reiniciar.

---

## 9. Monitoreo

- **Application Insights** — habilitarlo desde el App Service → *Application Insights* → *Turn on*.
- **Log stream** — en la sección *Monitoring → Log stream* del App Service.
- **Backups SQLite** — copiar `/home/data/prepdiplomacia.db` a Azure Files o Storage de forma periódica con un Logic App.

---

## 10. Backup de la base SQLite

Copia manual desde Kudu:

```bash
cd /home/data
cp prepdiplomacia.db prepdiplomacia-$(date +%Y%m%d).db
```

Para automatizar, crear un **Azure Function** o **Logic App** que copie el archivo a Blob Storage cada noche.

---

## 11. Troubleshooting frecuente

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| `500.30 ANCM In-Process Start Failure` al desplegar | Falta Application Settings críticos | Revisar logs en `Log stream` |
| Stripe no activa la inscripción | Webhook mal configurado o secret incorrecto | Probar en Stripe → *Webhook → Send test webhook* |
| Mails no llegan | App Password expirada o 2FA desactivado | Generar nueva App Password |
| Imágenes desaparecen tras deploy | Uploads en `wwwroot` (no persistente) | Mover a `/home/data` o Blob Storage |

---

## 12. Costos estimados (USD/mes)

| Recurso | Plan | Costo |
|---------|------|-------|
| App Service B1 Linux | Basic | ~ 13 |
| Azure SQL Basic (cuando se migre) | DTU 5 | ~ 5 |
| Application Insights | Pay-as-you-go | < 5 |
| Storage (uploads) | Hot tier | < 1 |
| **Total inicial estimado** | | **~ 13 – 25** |

---

¿Dudas? Escribir a `prepdiplomaciauy@gmail.com` con los logs adjuntos.
