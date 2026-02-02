using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StepikAnalytics.Desktop.Services.ApiClient;
using StepikAnalytics.Shared.Dtos;

namespace StepikAnalytics.Desktop.ViewModels;

public partial class CoursesViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;
    private readonly Action<Guid, string> _onCourseSelected;

    [ObservableProperty]
    private ObservableCollection<CourseDto> _courses = new();

    [ObservableProperty]
    private string _newCourseInput = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isAddingCourse;

    public CoursesViewModel(IApiClient apiClient, Action<Guid, string> onCourseSelected)
    {
        _apiClient = apiClient;
        _onCourseSelected = onCourseSelected;
        _ = LoadCoursesAsync();
    }

    [RelayCommand]
    private async Task LoadCoursesAsync(CancellationToken ct = default)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var courses = await _apiClient.GetCoursesAsync(ct);
            Courses = new ObservableCollection<CourseDto>(courses);

            // Sync all courses on load
            foreach (var course in courses)
            {
                _ = _apiClient.SyncCourseAsync(course.Id, ct);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddCourseAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(NewCourseInput))
        {
            ErrorMessage = "Введите ID или URL курса";
            return;
        }

        try
        {
            IsAddingCourse = true;
            ErrorMessage = null;

            var course = await _apiClient.AddCourseAsync(NewCourseInput, ct);
            if (course != null)
            {
                Courses.Insert(0, course);
                NewCourseInput = string.Empty;
            }
            else
            {
                ErrorMessage = "Не удалось добавить курс. Проверьте ID или URL.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsAddingCourse = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        try
        {
            var success = await _apiClient.DeleteCourseAsync(courseId, ct);
            if (success)
            {
                var course = Courses.FirstOrDefault(c => c.Id == courseId);
                if (course != null)
                    Courses.Remove(course);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка удаления: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectCourse(CourseDto course)
    {
        _onCourseSelected(course.Id, course.Title);
    }
}
