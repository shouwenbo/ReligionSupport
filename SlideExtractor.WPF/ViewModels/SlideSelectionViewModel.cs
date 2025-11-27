using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlideExtractor.WPF.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace SlideExtractor.WPF.ViewModels;

public partial class SlideSelectionViewModel : ObservableObject
{
	private ICollectionView? _view;
	private ObservableCollection<SlideModel> _source = new();

	[ObservableProperty] private string _filter = "All";
	public ICollectionView FilteredSlides => _view ??= CollectionViewSource.GetDefaultView(_source);

	public void Attach(ObservableCollection<SlideModel> source)
	{
		_source = source;
		_view = CollectionViewSource.GetDefaultView(_source);
		_view.Filter = FilterPredicate;
		OnPropertyChanged(nameof(FilteredSlides));
	}

	[RelayCommand] private void SelectAll() => SetSelection(true);
	[RelayCommand] private void DeselectAll() => SetSelection(false);
	[RelayCommand] private void InvertSelection()
	{
		foreach (var slide in FilteredSlides.Cast<SlideModel>())
		{
			slide.IsSelected = !slide.IsSelected;
		}
	}

	partial void OnFilterChanged(string value) => _view?.Refresh();

	private void SetSelection(bool flag)
	{
		foreach (var slide in FilteredSlides.Cast<SlideModel>())
		{
			slide.IsSelected = flag;
		}
	}

	private bool FilterPredicate(object obj) => obj is SlideModel slide && Filter switch
	{
		"HasText" => slide.HasText,
		"NoText" => !slide.HasText,
		_ => true
	};
}
