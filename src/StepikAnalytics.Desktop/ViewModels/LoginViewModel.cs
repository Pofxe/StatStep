using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StepikAnalytics.Desktop.Services.ApiClient;

namespace StepikAnalytics.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly Action<string> _onLoginSuccess;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRegistering;

    [ObservableProperty]
    private string? _email;

    public LoginViewModel(IApiClient apiClient, Action<string> onLoginSuccess)
    {
        _apiClient = apiClient;
        _onLoginSuccess = onLoginSuccess;
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите имя пользователя и пароль";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var result = await _apiClient.LoginAsync(Username, Password, ct);
            if (result != null)
            {
                _onLoginSuccess(result.Username);
            }
            else
            {
                ErrorMessage = "Неверное имя пользователя или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите имя пользователя и пароль";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Пароль должен быть не менее 6 символов";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var success = await _apiClient.RegisterAsync(Username, Password, Email, ct);
            if (success)
            {
                // Auto login after registration
                var result = await _apiClient.LoginAsync(Username, Password, ct);
                if (result != null)
                {
                    _onLoginSuccess(result.Username);
                }
            }
            else
            {
                ErrorMessage = "Пользователь с таким именем уже существует";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegistering = !IsRegistering;
        ErrorMessage = null;
    }
}
