# Clinica.web

## Requisitos
- .NET 8 SDK (o Runtime si solo vas a ejecutar)
- SQL Server Express **LocalDB** (instancia `MSSQLLocalDB`)

## Ejecución
- Abrir la solución `Clinica.sln` en Visual Studio y ejecutar el proyecto `Clinica.Web`, o
- Por consola:
  - `dotnet run --project Clinica.Web`

Al iniciar, la aplicación:
- Verifica si existe la base de datos configurada en `Clinica.Web/appsettings.json`.
- Si no existe, la crea y aplica migraciones (dominio + Identity).
- Crea roles iniciales y un usuario administrador.

## Usuario administrador inicial
- Email: `admin@clinica.local`
- Password: `Admin123!`

## Configuración en Producción
Para desplegar la aplicación en producción, la cadena de conexión a la base de datos debe inyectarse de forma segura usando la variable de entorno `CLINICA_DB_CONNECTION`.
Por seguridad, **nunca** guardes credenciales de bases de datos productivas en los archivos `appsettings.json` o `appsettings.Production.json`.
