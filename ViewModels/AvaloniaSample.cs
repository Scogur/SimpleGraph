using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace SimpleGraph.ViewModels;
public class ViewModel : ObservableValue
{
    public ISeries[] Series { get; set; } = [];
}