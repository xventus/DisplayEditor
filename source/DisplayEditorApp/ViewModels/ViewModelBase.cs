using CommunityToolkit.Mvvm.ComponentModel;

namespace DisplayEditorApp.ViewModels;

/// <summary>
/// Base class for all ViewModels in the DisplayEditor application.
/// Provides fundamental MVVM infrastructure through inheritance from CommunityToolkit's ObservableObject.
/// 
/// This base class serves as:
/// - Foundation for property change notification (INotifyPropertyChanged)
/// - Common inheritance point for all application ViewModels
/// - Future extensibility point for shared ViewModel functionality
/// - Consistent MVVM pattern implementation across the application
/// 
/// Inherited by:
/// - MainViewModel: Main application logic and file management
/// - EditDialogViewModel: File editing dialog functionality
/// 
/// Key inherited functionality from ObservableObject:
/// - SetProperty&lt;T&gt;(ref T field, T value, [CallerMemberName] string propertyName): 
///   Sets property value and raises PropertyChanged if value differs
/// - OnPropertyChanged([CallerMemberName] string propertyName): 
///   Manually raises PropertyChanged event for computed properties
/// - PropertyChanging/PropertyChanged events for UI binding notifications
/// </summary>
/// <remarks>
/// This class follows the MVVM pattern where:
/// - ViewModels handle presentation logic and state management
/// - Views (XAML) bind to ViewModel properties and commands
/// - Models represent data and business logic
/// 
/// The empty implementation allows for future expansion of common ViewModel
/// functionality such as:
/// - Common validation logic
/// - Shared error handling
/// - Base command implementations
/// - Common property patterns
/// - Logging or diagnostic capabilities
/// 
/// Example usage in derived classes:
/// <code>
/// private string _myProperty = string.Empty;
/// public string MyProperty
/// {
///     get => _myProperty;
///     set => SetProperty(ref _myProperty, value);
/// }
/// </code>
/// </remarks>
public class ViewModelBase : ObservableObject
{
    // Currently empty - provides foundation for future shared ViewModel functionality
    // All functionality is inherited from CommunityToolkit.Mvvm.ComponentModel.ObservableObject
}
