# Prep Diplomacia

Plataforma educativa para la preparación al **Concurso de Ingreso al Servicio Exterior del Uruguay**, dirigida por Carolina Techera.

> **Stack:** ASP.NET Core 8 MVC · ASP.NET Identity · Entity Framework Core (SQLite o SQL Server) · Stripe · Mailchimp · MailKit (SMTP Gmail).

---

## 📑 Índice

1. [Características](#características)
2. [Estructura del proyecto](#estructura-del-proyecto)
3. [Requisitos previos](#requisitos-previos)
4. [Setup local paso a paso](#setup-local-paso-a-paso)
5. [Configuración de servicios externos](#configuración-de-servicios-externos)
6. [Cuenta de administración inicial](#cuenta-de-administración-inicial)
7. [Migración a SQL Server](#migración-a-sql-server)
8. [Deploy en Azure](#deploy-en-azure)
9. [Estructura de la base de datos](#estructura-de-la-base-de-datos)

---

## Características

- **Sitio público:** Inicio, El Programa, Sobre Prep, Blog (paginado, categorías, tags, comentarios moderados, búsqueda), Contacto, Términos y Política de Privacidad (Ley 18.331 Uruguay).
- **Inscripción y pago:** Selector de planes, Stripe Checkout (pago único o cuotas mensuales con corte automático), webhook con idempotencia, alta automática de cuenta de alumno.
- **Newsletter:** Doble opt-in con Mailchimp; la base local es la fuente de verdad.
- **Área de alumnos:** Acceso protegido por rol y por flag de pago confirmado.
- **Panel admin:** Dashboard, CRUD de blog, moderación de comentarios, edición de bloques de contenido del sitio (sin tocar código), gestión de categorías y tags, listado de inscripciones con detalle de pagos, mensajes de contacto, suscriptores.
- **Seguridad:** Antiforgery en formularios, lockout tras 5 intentos fallidos, validación de subida de imágenes (whitelist + GUID + tamaño máx. 5 MB), idempotencia de webhooks Stripe vía tabla `EventosStripe`.

---

## Estructura del proyecto

```
PrepDiplomacia/
├── PrepDiplomacia.sln
├── src/
│   ├── PrepDiplomacia.Domain/         ← Entidades, enums, constantes (sin dependencias)
│   ├── PrepDiplomacia.Infrastructure/ ← EF Core, Identity, Stripe, MailKit, Mailchimp, servicios
│   └── PrepDiplomacia.Web/            ← ASP.NET Core MVC (controllers, vistas, wwwroot)
└── docs/
    ├── README.md         ← este archivo
    └── DEPLOY-AZURE.md   ← guía de publicación
```

---

## Requisitos previos

- **Visual Studio 2022/2026 Community** o **Rider** con soporte .NET 8.
- **.NET 8 SDK** instalado (`dotnet --version` debería responder `8.x`).
- (Opcional) **Stripe CLI** para probar webhooks localmente: <https://stripe.com/docs/stripe-cli>.

---

## Setup local paso a paso

### 1. Clonar y restaurar

```bash
git clone <repo-url> PrepDiplomacia
cd PrepDiplomacia
dotnet restore
```

### 2. Configurar secretos locales

Desde la carpeta del proyecto Web (`src/PrepDiplomacia.Web`), inicializar el almacén de **User Secrets**:

```bash
cd src/PrepDiplomacia.Web
dotnet user-secrets init
```

Luego cargar los valores reales (los que figuran a continuación son ejemplos):

```bash
# Email (App Password de Gmail)
dotnet user-secrets set "Email:Smtp:Usuario"  "prepdiplomaciauy@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "xxxx xxxx xxxx xxxx"

# Stripe (claves de TEST mientras se desarrolla)
dotnet user-secrets set "Stripe:SecretKey"      "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Stripe:WebhookSecret"  "whsec_..."
dotnet user-secrets set "Stripe:SiteUrl"        "https://localhost:5001"

# Mailchimp (opcional; si se omite, las suscripciones quedan solo en BD local)
dotnet user-secrets set "Mailchimp:ApiKey"     "abc123..."
dotnet user-secrets set "Mailchimp:DataCenter" "us21"
dotnet user-secrets set "Mailchimp:AudienceId" "abcd1234"

# Contraseña inicial del admin (se aplica solo en el primer arranque)
dotnet user-secrets set "Admin:Password" "TuPasswordSegura#2027"
```

> 🔐 **Nunca** commitees estos valores. Los User Secrets se guardan fuera del repo.

### 3. Crear la migración inicial

```bash
# Desde la raíz del repo
dotnet ef migrations add InicialSqlite \
    -p src/PrepDiplomacia.Infrastructure \
    -s src/PrepDiplomacia.Web \
    -o Data/Migrations
```

Si nunca instalaste la herramienta global de EF Core:

```bash
dotnet tool install --global dotnet-ef
```

### 4. Ejecutar

```bash
cd src/PrepDiplomacia.Web
dotnet run
```

Abrir <https://localhost:5001>. La primera ejecución:

1. Aplica la migración (la base SQLite se crea en `App_Data/prepdiplomacia.db`).
2. Crea los roles `Admin` y `Alumno`.
3. Crea el usuario administrador inicial (Carolina).
4. Pobla los bloques de contenido editables y los planes de ejemplo.

---

## Configuración de servicios externos

### Email — Gmail App Password

1. Activar la verificación en dos pasos en la cuenta de Google.
2. Ir a <https://myaccount.google.com/apppasswords>.
3. Crear una contraseña de aplicación llamada «Prep Diplomacia».
4. Pegar la contraseña en el secreto `Email:Smtp:Password`.

### Stripe

1. Crear cuenta en <https://dashboard.stripe.com>.
2. Copiar `Secret key` y `Publishable key` (modo Test).
3. Crear el webhook apuntando a `https://localhost:5001/pago/webhook` (con `stripe listen` localmente) o `https://prepdiplomacia.uy/pago/webhook` en producción. Eventos a escuchar:
   - `checkout.session.completed`
   - `invoice.paid`
   - `invoice.payment_failed`
4. Copiar el `Signing secret` del webhook al secreto `Stripe:WebhookSecret`.

Para probar localmente:

```bash
stripe listen --forward-to https://localhost:5001/pago/webhook
```

### Mailchimp (opcional)

1. Crear una API Key desde <https://admin.mailchimp.com/account/api/>.
2. El **Data Center** son las letras finales de la API Key (ej. `us21`).
3. El **Audience ID** se obtiene desde *Audience → Settings → Audience name and defaults*.

Si no se configura Mailchimp, las suscripciones se guardan únicamente en la base local.

---

## Cuenta de administración inicial

Por defecto, se crea automáticamente:

- **Email:** `prepdiplomaciauy@gmail.com`
- **Contraseña:** la del secreto `Admin:Password` (o, si no se setea, `CambiarEnPrimerLogin#2027`).

🔐 **Después del primer login, ir a `/cuenta/cambiar-password`** y cambiar la contraseña de inmediato.

---

## Migración a SQL Server

Cuando el proyecto crezca, basta con cambiar dos valores en `appsettings.json` (o, mejor, en variables de entorno de Azure):

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "Default": "Server=tcp:tu-server.database.windows.net,1433;Database=PrepDiplomacia;User Id=..;Password=..;Encrypt=true;"
  }
}
```

Y crear una nueva migración para SQL Server:

```bash
dotnet ef migrations add InicialSqlServer \
    -p src/PrepDiplomacia.Infrastructure \
    -s src/PrepDiplomacia.Web \
    -o Data/Migrations \
    -- --provider SqlServer
```

> 💡 EF Core puede aplicar la misma migración SQLite a SQL Server pero hay diferencias menores (por ejemplo, `decimal` se mapea distinto). Lo recomendado es generar migraciones específicas por proveedor.

---

## Deploy en Azure

Ver `docs/DEPLOY-AZURE.md` para la guía paso a paso.

---

## Estructura de la base de datos

| Tabla              | Propósito |
|--------------------|-----------|
| `Usuarios`         | ASP.NET Identity (Carolina + Alumnos) |
| `Roles`            | `Admin`, `Alumno` |
| `Posts`            | Publicaciones del blog |
| `Categorias`       | Categorías del blog |
| `Tags`             | Etiquetas |
| `PostBlogTags`     | Tabla puente N:N posts↔tags |
| `Comentarios`      | Comentarios del blog (moderados) |
| `BloquesContenido` | Bloques de texto editables del sitio |
| `Suscriptores`     | Newsletter |
| `Mensajes`         | Mensajes del formulario de contacto |
| `Planes`           | Planes de curso ofrecidos |
| `Inscripciones`    | Formularios de inscripción |
| `Pagos`            | Movimientos de cobro (pago único + cuotas) |
| `EventosStripe`    | Idempotencia de webhooks |

---

## Soporte y contacto

Para consultas técnicas: `prepdiplomaciauy@gmail.com`.
