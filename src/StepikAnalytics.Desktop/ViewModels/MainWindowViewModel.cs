using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StepikAnalytics.Desktop.Services.ApiClient;

namespace StepikAnalytics.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private string? _username;

    public MainWindowViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        ShowLogin();
    }

    [RelayCommand]
    private void ShowLogin()
    {
        CurrentView = new LoginViewModel(_apiClient, OnLoginSuccess);
    }

    [RelayCommand]
    private void ShowCourses()
    {
        CurrentView = new CoursesViewModel(_apiClient, ShowDashboard);
    }

    [RelayCommand]
    private void Logout()
    {
        _apiClient.Token = null;
        IsAuthenticated = false;
        Username = null;
        ShowLogin();
    }

    private void OnLoginSuccess(string username)
    {
        IsAuthenticated = true;
        Username = username;
        ShowCourses();
    }

    private void ShowDashboard(Guid courseId, string courseTitle)
    {
        CurrentView = new CourseDashboardViewModel(_apiClient, courseId, courseTitle, ShowCourses);
    }
}
