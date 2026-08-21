using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class JointDeflectionSettingsViewModel : ViewModelBase
{
    private readonly BendSettings _source;
    private readonly List<JointDeflectionSpec> _otherRows = new();
    private string _activeJointType;
    private JointApplicationMode _applicationMode;
    public JointDeflectionSettingsViewModel(BendSettings source)
    {
        _source = source; _activeJointType = source.ActiveJointType; _applicationMode = source.ApplicationMode;
        AddKpCommand = new RelayCommand(_ => KpRows.Add(new JointDeflectionRow { JointType = JointTypeCatalog.KpMechanical }));
        RemoveKpCommand = new RelayCommand(_ => { if (SelectedKpRow is not null) KpRows.Remove(SelectedKpRow); });
        AddTytonCommand = new RelayCommand(_ => TytonRows.Add(new JointDeflectionRow { JointType = JointTypeCatalog.Tyton }));
        RemoveTytonCommand = new RelayCommand(_ => { if (SelectedTytonRow is not null) TytonRows.Remove(SelectedTytonRow); });
        RestoreCommand = new RelayCommand(_ => Load(JointDeflectionTable.Default));
        SaveCommand = new RelayCommand(_ => Confirm()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Load(source.JointDeflections);
    }
    /// <summary>KP 메커니컬 조인트 행. 조인트 종류가 컬렉션 자체로 결정되므로 행에 Joint Type 열이 없다(2026-08-21).</summary>
    public ObservableCollection<JointDeflectionRow> KpRows { get; } = new();
    public ObservableCollection<JointDeflectionRow> TytonRows { get; } = new();
    public IReadOnlyList<string> JointTypes => JointTypeCatalog.All;
    public string KpJointTypeName => JointTypeCatalog.KpMechanical;
    public string TytonJointTypeName => JointTypeCatalog.Tyton;
    public string ActiveJointType { get => _activeJointType; set => SetProperty(ref _activeJointType, value); }
    public JointApplicationMode ApplicationMode { get => _applicationMode; set { if (SetProperty(ref _applicationMode, value)) { OnPropertyChanged(nameof(IsSingleJoint)); OnPropertyChanged(nameof(IsBothJoints)); } } }
    public bool IsSingleJoint { get => ApplicationMode == JointApplicationMode.SingleJoint; set { if (value) ApplicationMode = JointApplicationMode.SingleJoint; } }
    public bool IsBothJoints { get => ApplicationMode == JointApplicationMode.BothJoints; set { if (value) ApplicationMode = JointApplicationMode.BothJoints; } }
    public JointDeflectionRow? SelectedKpRow { get; set; }
    public JointDeflectionRow? SelectedTytonRow { get; set; }
    public ICommand AddKpCommand { get; } public ICommand RemoveKpCommand { get; }
    public ICommand AddTytonCommand { get; } public ICommand RemoveTytonCommand { get; }
    public ICommand RestoreCommand { get; } public ICommand SaveCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public BendSettings? Result { get; private set; }
    private void Load(JointDeflectionTable table)
    {
        KpRows.Clear(); TytonRows.Clear(); _otherRows.Clear();
        foreach (var x in table.Entries)
        {
            if (string.Equals(x.JointType, JointTypeCatalog.KpMechanical, StringComparison.OrdinalIgnoreCase))
                KpRows.Add(new JointDeflectionRow { JointType = JointTypeCatalog.KpMechanical, DiameterMm = x.DiameterMm, AllowableDeg = x.AllowableDeg });
            else if (string.Equals(x.JointType, JointTypeCatalog.Tyton, StringComparison.OrdinalIgnoreCase))
                TytonRows.Add(new JointDeflectionRow { JointType = JointTypeCatalog.Tyton, DiameterMm = x.DiameterMm, AllowableDeg = x.AllowableDeg });
            else
                _otherRows.Add(x);
        }
    }
    /// <summary>이 탭의 편집값을 <paramref name="baseSettings"/> 위에 반영한다. 규격표 창에 3번째 탭으로 얹혀 있어(2026-08-20)
    /// 독립 창으로 열릴 때 쓰던 <see cref="Confirm"/>(SaveCommand)과 별개로, 바깥쪽 [확인] 한 번에 함께 저장하기 위한 진입점이다.</summary>
    public BendSettings BuildResult(BendSettings baseSettings) => baseSettings with
    {
        JointDeflections = new JointDeflectionTable(
            KpRows.Where(x => x.DiameterMm > 0 && x.AllowableDeg > 0)
                .Select(x => new JointDeflectionSpec(JointTypeCatalog.KpMechanical, x.DiameterMm, x.AllowableDeg))
                .Concat(TytonRows.Where(x => x.DiameterMm > 0 && x.AllowableDeg > 0)
                    .Select(x => new JointDeflectionSpec(JointTypeCatalog.Tyton, x.DiameterMm, x.AllowableDeg)))
                .Concat(_otherRows)
                .ToList()),
        ActiveJointType = ActiveJointType,
        ApplicationMode = ApplicationMode
    };
    private void Confirm() { Result = BuildResult(_source); CloseAction?.Invoke(); }
}
