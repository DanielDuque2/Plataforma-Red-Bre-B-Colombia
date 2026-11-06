namespace Breb.App.Servicios;

public sealed class ConfiguracionApi
{
    private const string ClavePreferencia = "BrebApiBaseUrl";
    private const string UrlEmulador = "http://10.0.2.2:5080";
    private const string UrlLocal = "http://127.0.0.1:5080";

    public ConfiguracionApi()
    {
        UrlBase = Preferences.Default.Get(
            ClavePreferencia,
            DeviceInfo.Platform == DevicePlatform.Android ? UrlEmulador : UrlLocal);
    }

    public string UrlBase { get; private set; }

    public bool IntentarGuardar(string? url, out string mensaje)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            mensaje = "Usa una URL base como http://192.168.1.25:5080.";
            return false;
        }

        UrlBase = uri.GetLeftPart(UriPartial.Authority);
        Preferences.Default.Set(ClavePreferencia, UrlBase);
        mensaje = $"API configurada: {UrlBase}";
        return true;
    }

    public Uri CrearUri(string ruta) =>
        new(new Uri(UrlBase.TrimEnd('/') + "/"), ruta.TrimStart('/'));
}
