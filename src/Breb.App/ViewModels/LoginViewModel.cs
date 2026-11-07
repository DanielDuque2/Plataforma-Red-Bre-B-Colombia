using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Breb.App.Servicios;

namespace Breb.App.ViewModels;

/// <summary>
/// Clase 19: la pantalla de entrada. Una sola vez, un solo token.
///
/// <para>
/// Es el ViewModel más corto de la app y hace la cosa más importante:
/// convierte usuario y clave en una sesión que las otras dos pantallas
/// consumen sin volver a preguntar nada.
/// </para>
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly ClienteBrebApi _api;
    private readonly SesionUsuario _sesion;
    private readonly ConfiguracionApi _configuracionApi;

    // Este ViewModel es transitorio, así que puede recibir el ClienteBrebApi
    // sin capturarlo. La sesión, que es singleton, no puede: ver el comentario
    // de ciclos de vida en SesionUsuario.
    public LoginViewModel(
        ClienteBrebApi api, SesionUsuario sesion, ConfiguracionApi configuracionApi)
    {
        _api = api;
        _sesion = sesion;
        _configuracionApi = configuracionApi;

        // Credenciales del laboratorio precargadas. En una app de verdad
        // esto no va aquí jamás; en clase ahorra escribir en un teclado de
        // emulador delante de treinta personas.
        //
        // Se entra como ana.operadora y no como luis.consulta porque el rol
        // operador sirve para las DOS pantallas: puede retener y también
        // leer saldos. Con luis.consulta el saldo funciona y la
        // transferencia devuelve 403.
        Usuario = "ana.operadora";
        Clave = "Operadora-2026";

        UrlApi = _configuracionApi.UrlBase;
        DireccionApi = _configuracionApi.Descripcion;

        // Si la app corre en un teléfono físico y nadie puso la IP de la
        // WiFi, decirlo AQUÍ, antes del primer intento. El alternativo es
        // un timeout de 10 s que parece lentitud del servidor.
        if (_configuracionApi.FaltaConfigurar)
            Estado = _configuracionApi.ComoConfigurar;
    }

    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private string clave = string.Empty;

    [ObservableProperty]
    private string estado = "Entre con su usuario del laboratorio.";

    [ObservableProperty]
    private string direccionApi = string.Empty;

    [ObservableProperty]
    private string urlApi = string.Empty;

    [ObservableProperty]
    private bool entrando;

    [ObservableProperty]
    private bool configurandoApi;

    public string? ErrorAlerta { get; private set; }

    /// <summary>
    /// Para deshabilitar el botón mientras se entra. El XAML no sabe negar
    /// un booleano sin un convertidor, y un convertidor para esto sería una
    /// clase entera para ahorrar dos líneas. Misma técnica que
    /// <c>NoHayPendientes</c> en <see cref="TransferenciaViewModel"/>.
    /// </summary>
    public bool NoEntrando => !Entrando && !ConfigurandoApi;
    public bool NoConfigurandoApi => !Entrando && !ConfigurandoApi;
    public string TextoBotonEntrar => Entrando ? "Conectando…" : "Entrar";
    public string TextoBotonApi => ConfigurandoApi ? "Configurando…" : "Configurar API";

    partial void OnEntrandoChanged(bool value)
    {
        OnPropertyChanged(nameof(NoEntrando));
        OnPropertyChanged(nameof(NoConfigurandoApi));
        OnPropertyChanged(nameof(TextoBotonEntrar));
    }

    partial void OnConfigurandoApiChanged(bool value)
    {
        OnPropertyChanged(nameof(NoEntrando));
        OnPropertyChanged(nameof(NoConfigurandoApi));
        OnPropertyChanged(nameof(TextoBotonApi));
    }

    public bool IntentarGuardarUrlApi(string url, out string mensaje)
    {
        if (!_configuracionApi.IntentarGuardar(url, out mensaje))
        {
            Estado = mensaje;
            return false;
        }

        UrlApi = _configuracionApi.UrlBase;
        DireccionApi = _configuracionApi.Descripcion;
        Estado = mensaje;
        return true;
    }

    [RelayCommand]
    private async Task EntrarAsync()
    {
        ErrorAlerta = null;

        if (Entrando || ConfigurandoApi) return;

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Clave))
        {
            RegistrarError("Usuario y clave son obligatorios.");
            return;
        }

        if (_configuracionApi.FaltaConfigurar)
        {
            RegistrarError(_configuracionApi.ComoConfigurar);
            return;
        }

        Entrando = true;
        Estado = "Pidiendo token…";
        try
        {
            var r = await _api.ObtenerTokenAsync(Usuario.Trim(), Clave);
            _sesion.Guardar(Usuario.Trim(), r);

            Estado = $"Bienvenido. Rol {r.Rol}, el token vence en " +
                     $"{r.DuracionMinutos:N0} minuto(s).";

            // Advertir SIN bloquear. Entrar con rol consulta es legítimo
            // —se puede ver el saldo— y el 403 que llega al intentar
            // transferir es la autorización funcionando, no una falla. Pero
            // más vale saberlo antes de la demo que durante.
            if (!_sesion.PuedeTransferir)
                Estado += $" Ojo: con el rol {r.Rol} se puede consultar el saldo, " +
                          "pero retener fondos va a devolver 403.";

            // Navegar desde el ViewModel. La alternativa purista es una
            // interfaz INavegacion inyectada; para dos pantallas no paga el
            // costo, y conviene saber que ésta es la línea que habría que
            // cambiar si algún día hay que probar esto sin un Shell vivo.
            await Shell.Current.GoToAsync("//MainPage");
        }
        catch (HttpRequestException)
        {
            RegistrarError(_configuracionApi.FaltaConfigurar
                ? _configuracionApi.ComoConfigurar
                : $"Sin conexión con la API en {_configuracionApi.Direccion}. " +
                  "¿Está corriendo, y el teléfono está en la misma WiFi?");
        }
        catch (TaskCanceledException)
        {
            // Un firewall no rechaza: deja esperando. Por eso el síntoma es
            // un timeout y no un "sin conexión", y por eso la prueba que
            // vale es abrir Swagger en el navegador DEL teléfono.
            RegistrarError($"Se agotó el tiempo esperando a {_configuracionApi.Direccion}. " +
                     "Un firewall no rechaza: deja esperando. Abra " +
                     $"{_configuracionApi.SwaggerParaProbar} en el navegador del teléfono: " +
                     "si esa página no carga, el problema no está en la app.");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("Login rechazado: HTTP 401"))
        {
            RegistrarError("Usuario o clave incorrectos. Verifica tus credenciales e inténtalo de nuevo.");
        }
        catch (Exception ex)
        {
            RegistrarError($"No se pudo iniciar sesión: {ex.Message}");
        }
        finally
        {
            Entrando = false;
        }
    }

    private void RegistrarError(string mensaje)
    {
        Estado = mensaje;
        ErrorAlerta = mensaje;
    }
}
