using Microsoft.Extensions.Logging;
using Breb.App.Servicios;
using Breb.App.ViewModels;

namespace Breb.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// La URL se puede cambiar desde la pestaña Saldo. El valor inicial
		// distingue el emulador Android del escritorio; en un teléfono físico
		// se configura la IPv4 del PC en la misma red Wi-Fi.
		builder.Services.AddSingleton<ConfiguracionApi>();

		builder.Services.AddHttpClient<ClienteBrebApi>(http =>
		{
			http.Timeout = TimeSpan.FromSeconds(10);
		});

		// La sesion del usuario: UN token para toda la app.
		//
		// Singleton porque es UNA sesion; dos instancias serian dos usuarios
		// distintos dentro de la misma app. Antes cada pantalla pedia su
		// propio token en cada operacion, y con usuarios distintos.
		builder.Services.AddSingleton<SesionUsuario>();

		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<LoginPage>();

		builder.Services.AddTransient<SaldoViewModel>();
		builder.Services.AddTransient<MainPage>();

		// Clase 18: la segunda pantalla. Sin registrar aqui el ViewModel y la
		// pagina, el Shell no puede construirla: la pestana sale en blanco y
		// no aparece ningun error a la vista.
		builder.Services.AddTransient<TransferenciaViewModel>();
		builder.Services.AddTransient<TransferenciaPage>();

		// La cola local. Singleton porque es UNA cola: dos instancias
		// escribiendo el mismo archivo se pisarian, y el candado que las
		// serializa vive dentro de la instancia.
		//
		// AppDataDirectory es la carpeta privada de la app que el sistema
		// RESPALDA y no borra al limpiar cache. Si esto apuntara a
		// CacheDirectory, Android podria borrar las transferencias
		// pendientes del usuario cuando necesitara espacio.
		builder.Services.AddSingleton(new ColaOperaciones(FileSystem.AppDataDirectory));

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
