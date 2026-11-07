using Breb.App.ViewModels;

namespace Breb.App;

// Sin lógica aquí: el ViewModel llega por DI y el binding hace todo el trabajo.
public partial class LoginPage : ContentPage
{
	public LoginPage(LoginViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnEntrarClicked(object? sender, EventArgs e)
	{
		var viewModel = (LoginViewModel)BindingContext;
		if (!viewModel.NoEntrando)
			return;

		await viewModel.EntrarCommand.ExecuteAsync(null);

		if (!string.IsNullOrWhiteSpace(viewModel.ErrorAlerta))
			await DisplayAlertAsync("No se pudo iniciar sesión", viewModel.ErrorAlerta, "Aceptar");
	}

	private void OnEntrarCompleted(object? sender, EventArgs e) =>
		OnEntrarClicked(sender, e);

	private async void OnConfigurarApiClicked(object? sender, EventArgs e)
	{
		var viewModel = (LoginViewModel)BindingContext;
		if (!viewModel.NoConfigurandoApi)
			return;

		viewModel.ConfigurandoApi = true;
		try
		{
			var url = await DisplayPromptAsync(
				"Configurar API",
				"Escribe la URL base. En un teléfono usa la IPv4 del PC, por ejemplo http://192.168.1.25:5080.",
				accept: "Guardar",
				cancel: "Cancelar",
				placeholder: "http://192.168.1.25:5080",
				keyboard: Keyboard.Url,
				initialValue: viewModel.UrlApi);

			if (url is null)
				return;

			if (!viewModel.IntentarGuardarUrlApi(url, out var mensaje))
			{
				await DisplayAlertAsync("URL no válida", mensaje, "Aceptar");
				return;
			}

			await DisplayAlertAsync("API configurada", mensaje, "Aceptar");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("No se pudo configurar la API", ex.Message, "Aceptar");
		}
		finally
		{
			viewModel.ConfigurandoApi = false;
		}
	}
}
