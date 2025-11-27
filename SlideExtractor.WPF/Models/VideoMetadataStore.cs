using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SlideExtractor.WPF.Models;

public partial class VideoMetadataStore : ObservableObject
{
	[ObservableProperty] private int _totalFrames;
	[ObservableProperty] private double _durationSeconds;
	[ObservableProperty] private double _fps;

	public TimeSpan Duration => TimeSpan.FromSeconds(Math.Max(0, DurationSeconds));

	partial void OnDurationSecondsChanged(double value) => OnPropertyChanged(nameof(Duration));
}
