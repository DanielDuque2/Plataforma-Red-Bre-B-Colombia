# Breb.App — la aplicación móvil de la Red Bre-B

Proyecto .NET MAUI creado en la **Clase 14 (Semana 11)** y dotado de arquitectura en la **Clase 15**. Es la referencia de lo que se entrega en el **Evaluable 3** (#28) el 21 de octubre.

Hoy la app hace una cosa completa: pide un token a `/token` con usuario y clave, consulta `/cuentas/{id}/saldo` con ese token en el encabezado `Bearer`, y muestra el saldo en pantalla. Nadie escribe `saldoLabel.Text = ...` en ninguna parte: eso lo hace el binding.

| Pieza | Archivo | Responsabilidad |
|---|---|---|
| Vista | `MainPage.xaml` | Solo describe qué se ve y a qué se enlaza |
| ViewModel | `ViewModels/SaldoViewModel.cs` | Propiedades observables, comandos y manejo de error |
| Servicio | `Servicios/ClienteBrebApi.cs` | Único lugar que conoce rutas, JSON y el token |
| Contratos | `Modelos/Contratos.cs` | Los DTOs de la API, copiados a mano |
| Composición | `MauiProgram.cs` | Registra el `HttpClient` y resuelve las dependencias |

Dos paquetes que **no** vienen con la plantilla y hacen falta:

```bash
dotnet add package CommunityToolkit.Mvvm
dotnet add package Microsoft.Extensions.Http
```

`AddHttpClient` llega gratis en una API ASP.NET Core, pero **no** en MAUI. Sin `Microsoft.Extensions.Http` el build falla con `CS1061: IServiceCollection does not contain a definition for 'AddHttpClient'`, y falla al final, después de restaurar y de compilar los recursos de Android.

---

El proyecto está en la solución `src/Breb.Platform.slnx`, junto a `Breb.Cuentas`.

> **¿Va a presentar el Evaluable 3?** Empiece por **[`GUIA-E3.md`](GUIA-E3.md)**: los cuatro pasos en orden, con la comprobación de cada uno y qué hacer cuando falla. Este LEEME es la referencia de fondo; la guía es para seguirla en la máquina.

---

## ⚠️ Antes de compilar: clone el repositorio en una ruta corta

```bash
git clone https://github.com/CSA-DanielVillamizar/Plataforma-Red-Bre-B-Colombia.git C:\breb
cd C:\breb
dotnet build .\src\Breb.App\Breb.App.csproj -f net10.0-android
```

**No lo compile desde una ruta larga.** `aapt2`, el compilador de recursos de Android, falla con el límite de longitud de ruta de Windows:

```
\\?\C:\Users\...\obj\Debug\net10.0-android\lp\103\jl\res\..\flat\103.flata :
    error APT2000: The device does not recognize the command. (22)
```

Medido: 155 caracteres de ruta → **40 errores**; 20 caracteres → **0 errores**. El texto del error no menciona rutas; la pista es el prefijo `\\?\`.

Si el build ya falló, el `obj/` queda con rutas que ni se pueden borrar con normalidad. Bórrelo desde una ruta corta.

---

## Qué tarda

| Operación | Medido |
|---|---|
| Primer build Android, en frío | **300,2 s** |
| Segundo build, sin cambios | **14,5 s** |
| Primer build Windows, en frío | 44,3 s |

Cinco minutos en el primer build es **normal**. Detalle en [`docs/mediciones/semana-11-maui.md`](../../docs/mediciones/semana-11-maui.md).

---

## Hablar con la API Bre-B desde el emulador

La API debe escuchar en todas las interfaces:

```bash
docker start breb-postgres breb-rabbitmq
cd ../Breb.Cuentas
dotnet run --urls http://0.0.0.0:5080
```

En el emulador, la dirección **no** es `localhost`:

| Desde el emulador, hacia… | Resultado |
|---|---|
| `http://localhost:5080` | Connection refused |
| `http://10.0.2.2:5080` | **HTTP 200** |

Dentro del emulador, `localhost` es el emulador. La máquina anfitriona está en `10.0.2.2`.

> En un **teléfono real** `10.0.2.2` no sirve: ahí se usa la IP del computador en la red WiFi.

Comprobación rápida **del camino de red** (no de la app):

```bash
adb shell am start -a android.intent.action.VIEW -d "http://10.0.2.2:5080/swagger/index.html"
```

## Conectar un teléfono Android por la misma Wi-Fi

1. Conecta el PC y el teléfono a la misma red Wi-Fi. Evita la red de invitados: algunos routers aíslan sus dispositivos entre sí.
2. En Windows, ejecuta `ipconfig` y copia la dirección **IPv4** del adaptador Wi-Fi, por ejemplo `192.168.1.25`. No uses `127.0.0.1` ni `10.0.2.2` en el teléfono.
3. En una terminal del PC, inicia PostgreSQL y RabbitMQ:

   ```powershell
   docker start breb-postgres breb-rabbitmq
   ```

4. En otra terminal, desde la raíz del repositorio, inicia la API escuchando en la red:

   ```powershell
   dotnet run --project .\src\Breb.Cuentas\Breb.Cuentas.csproj -- --urls http://0.0.0.0:5080
   ```

5. Si Windows Defender Firewall pregunta, permite el acceso **solo en redes privadas**. Si no aparece el aviso y el teléfono no conecta, abre PowerShell como administrador y crea una regla limitada a la red local confiable:

   ```powershell
   New-NetFirewallRule -DisplayName "Breb API 5080 (LAN)" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5080 -Profile Private -RemoteAddress LocalSubnet
   ```

   Para quitarla después de la demo:

   ```powershell
   Remove-NetFirewallRule -DisplayName "Breb API 5080 (LAN)"
   ```

6. Desde el navegador del teléfono abre `http://<IPv4-del-PC>:5080/swagger/index.html`, por ejemplo `http://192.168.1.25:5080/swagger/index.html`. Si no carga, revisa la IP, la regla de firewall y que ambos estén en la misma red.
7. Abre Breb.App y ve a **Saldo**. En **URL de la API** escribe `http://<IPv4-del-PC>:5080`, pulsa **Guardar dirección de API** y luego **Consultar saldo**. La dirección se guarda en el teléfono; no hay que recompilar la app cuando cambia la IP.
8. En **Transferir**, prueba el envío. Para comprobar el modo offline, toca **Transferir** y activa modo avión mientras la app está pidiendo el token o enviando la retención. Verifica el aviso de operación guardada y la llave en pantalla; luego desactiva modo avión. Al recuperar Internet la app reenvía automáticamente la operación con la misma llave y la cola debe quedar vacía. Si el dispositivo no notifica el cambio de conectividad, usa **Reenviar lo pendiente**; no pulses **Transferir** otra vez para reintentar.

El proyecto permite HTTP sin cifrar **solo en Debug** para este laboratorio. Usa esta configuración únicamente en una Wi-Fi confiable, porque las credenciales y los tokens viajan sin TLS. No habilites HTTP en claro para una publicación de producción.

### Confirmar el abono simulado del banco destino

La app confirma la **retención**; el abono final lo confirma el servicio del banco destino. No se incluye la credencial de ese servicio en la app móvil. La saga compensa si no recibe el abono en **15 segundos**, así que prepara el token antes de enviar la transferencia:

```powershell
$api = "http://<IPv4-del-PC>:5080"
$tokenBanco = (Invoke-RestMethod -Method Post -Uri "$api/token" `
  -ContentType "application/json" `
  -Body (@{ usuario = "core-bancario"; clave = "CoreBancario-2026" } | ConvertTo-Json)).token
```

Después de tocar **Transferir**, copia inmediatamente la llave mostrada en la app y confirma el abono:

```powershell
$transferenciaId = "<llave-mostrada-en-la-app>"
Invoke-WebRequest -Method Post `
  -Uri "$api/transferencias/$transferenciaId/confirmar-abono" `
  -Headers @{ Authorization = "Bearer $tokenBanco" }
```

La API devuelve `202 Accepted` cuando publica la confirmación. La pestaña **Saldo** permite consultar el saldo actualizado. Para distinguir el abono completado de una compensación, verifica en PostgreSQL que la fila de esa llave tenga `Liquidada = true`; `Liberada = true` significa que la saga devolvió los fondos, no que completó el abono:

```sql
SELECT "TransferenciaId", "MontoUVB", "Liquidada", "Liberada"
FROM "Retenciones"
WHERE "TransferenciaId" = '<llave-mostrada-en-la-app>';
```

Esta cuenta de servicio y su contraseña son únicamente para el entorno de laboratorio.

--- 

## ⚠️ Y una cuarta trampa: Android bloquea el HTTP sin cifrar

**Que la URL abra en el navegador del emulador NO significa que la app pueda alcanzarla.**

Desde API 28, Android rechaza el tráfico HTTP en claro para las aplicaciones. Nuestra API de laboratorio usa HTTP sin TLS, así que el `HttpClient` de la app fallaría **aunque el permiso `INTERNET` esté concedido y aunque el navegador cargue la página sin problema**. El navegador tiene su propia política de red; la de la aplicación es independiente.

Ya está resuelto en `Platforms/Android/MainApplication.cs`:

```csharp
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
```

**Solo en Debug**, a propósito: una entrega que acepte HTTP en claro en Release estaría exponiendo tokens y saldos en texto plano.

---

## ⚠️ La quinta trampa: el APK de Debug no se puede instalar a mano

Esta es la que más duele, porque la app **compila sin un solo error** y aun así no abre.

Quien tome el `.apk` de `bin/Debug/net10.0-android/` y lo instale con `adb install` —o lo pase a un teléfono y lo abra— ve que la aplicación se cierra de inmediato. En pantalla no hay información: solo "la aplicación se detuvo". En `adb logcat` sí está la razón:

```
F monodroid: No assemblies found in
    '/data/user/0/com.companyname.breb.app/files/.__override__/x86_64'
    or '<unavailable>'. Assuming this is part of Fast Deployment. Exiting...
F monodroid: Abort at monodroid-glue.cc:757
F libc    : Fatal signal 6 (SIGABRT)
```

**Por qué.** En Debug, .NET para Android usa *Fast Deployment*: para que el ciclo de compilar-y-probar sea corto, los ensamblados .NET **no se empaquetan dentro del APK**. Los copia aparte el propio `dotnet build -t:Run`, a una carpeta privada de la aplicación. El APK que queda en `bin/` está incompleto a propósito, y por sí solo no arranca.

**Las dos salidas:**

```bash
# A) Desplegar con la herramienta, que además empuja los ensamblados
dotnet build -f net10.0-android -t:Run

# B) Generar un APK que se baste a sí mismo (para repartir o instalar a mano)
dotnet build -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true
```

Para el **Evaluable 3**, si el squad entrega un `.apk`, tiene que ser uno de los de la opción B —o un build de `Release`, que ya embebe los ensamblados—. Un APK de Debug suelto se ve bien, pesa lo mismo, y no abre en la máquina de nadie más.

> Un síntoma que despista: cuando la app aborta así en bucle, Android muestra **"Process system isn't responding"**. Parece que el emulador está lento o dañado; lo que pasa es que el sistema está reiniciando el proceso una y otra vez. El diálogo no habla de la app, pero la causa es la app.

---

## Plataformas

| Target framework | ¿Se compila en Windows? |
|---|---|
| `net10.0-android` | Sí |
| `net10.0-windows10.0.19041.0` | Sí |
| `net10.0-ios` | No — necesita un Mac |
| `net10.0-maccatalyst` | No — necesita un Mac |

En este curso se usa **Android**, porque el Evaluable 4 exige un teléfono real.
