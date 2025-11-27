using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace SlideExtractor.WPF.Models;

/// <summary>表示去重后的幻灯片帧。</summary>
public partial class SlideModel : ObservableObject
{
	public Guid Id { get; init; } = Guid.NewGuid();
	public int FrameIndex { get; init; }
	public TimeSpan Timestamp { get; init; }
	public string ImagePath { get; set; } = string.Empty;
	public string ThumbnailPath { get; set; } = string.Empty;
	public double SimilarityScore { get; set; }
	public ObservableCollection<string> Annotations { get; } = new();

	[ObservableProperty] private string _ocrText = string.Empty;
	[ObservableProperty] private bool _isSelected;

	public bool HasText => !string.IsNullOrWhiteSpace(OcrText);
}
