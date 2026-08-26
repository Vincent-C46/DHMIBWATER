using DHBIMWATER.Core.Structures;
using DHBIMWATER.UI.Base;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class ConcreteMaterialRowViewModel : ViewModelBase
{
    private int _maxAggregateSize = 25;
    private int _compressiveStrength;
    private int _slump = 120;

    public ConcreteMaterialRowViewModel(string memberName, int defaultCompressiveStrength = 24)
    {
        MemberName = memberName;
        _compressiveStrength = defaultCompressiveStrength;
    }

    public string MemberName { get; }
    public int MaxAggregateSize { get => _maxAggregateSize; set { _maxAggregateSize = value; OnPropertyChanged(nameof(MaxAggregateSize)); OnPropertyChanged(nameof(MaterialName)); } }
    public int CompressiveStrength { get => _compressiveStrength; set { _compressiveStrength = value; OnPropertyChanged(nameof(CompressiveStrength)); OnPropertyChanged(nameof(MaterialName)); } }
    public int Slump { get => _slump; set { _slump = value; OnPropertyChanged(nameof(Slump)); OnPropertyChanged(nameof(MaterialName)); } }
    public string MaterialName => ToConcreteSpec().MaterialName;
    public ConcreteSpec ToConcreteSpec() => new(MaxAggregateSize, CompressiveStrength, Slump);
}
