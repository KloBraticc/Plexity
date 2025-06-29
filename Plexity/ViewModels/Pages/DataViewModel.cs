using System.Windows.Media;
using Plexity.Models;
using Wpf.Ui.Abstractions.Controls;

namespace Plexity.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        public Task OnNavigatedToAsync()
        {

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        }
    }

