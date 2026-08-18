using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class JointDeflectionSettingsViewModel : ViewModelBase
{
    private readonly BendSettings _source;
    private string _activeJointType;
    private JointApplicationMode _applicationMode;
    public JointDeflectionSettingsViewModel(BendSettings source)
    {
        _source = source; _activeJointType = source.ActiveJointType; _applicationMode = source.ApplicationMode;
        AddCommand = new RelayCommand(_ => Rows.Add(new JointDeflectionRow()));
        RemoveCommand = new RelayCommand(_ => { if (SelectedRow is not null) Rows.Remove(SelectedRow); });
        RestoreCommand = new RelayCommand(_ => Load(JointDeflectionTable.Default));
        SaveCommand = new RelayCommand(_ => Confirm()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Load(source.JointDeflections);
    }
    public ObservableCollection<JointDeflectionRow> Rows { get; } = new();
    public IReadOnlyList<string> JointTypes => JointTypeCatalog.All;
    public string ActiveJointType { get => _activeJointType; set => SetProperty(ref _activeJointType, value); }
    public JointApplicationMode ApplicationMode { get => _applicationMode; set { if (SetProperty(ref _applicationMode, value)) { OnPropertyChanged(nameof(IsSingleJoint)); OnPropertyChanged(nameof(IsBothJoints)); } } }
    public bool IsSingleJoint { get => ApplicationMode == JointApplicationMode.SingleJoint; set { if (value) ApplicationMode = JointApplicationMode.SingleJoint; } }
    public bool IsBothJoints { get => ApplicationMode == JointApplicationMode.BothJoints; set { if (value) ApplicationMode = JointApplicationMode.BothJoints; } }
    public JointDeflectionRow? SelectedRow { get; set; }
    public ICommand AddCommand { get; } public ICommand RemoveCommand { get; } public ICommand RestoreCommand { get; } public ICommand SaveCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public BendSettings? Result { get; private set; }
    private void Load(JointDeflectionTable table) { Rows.Clear(); foreach (var x in table.Entries) Rows.Add(new JointDeflectionRow { JointType = x.JointType, DiameterMm = x.DiameterMm, AllowableDeg = x.AllowableDeg }); }
    private void Confirm() { Result = _source with { JointDeflections = new JointDeflectionTable(Rows.Where(x => x.DiameterMm > 0 && x.AllowableDeg > 0).Select(x => new JointDeflectionSpec(x.JointType, x.DiameterMm, x.AllowableDeg)).ToList()), ActiveJointType = ActiveJointType, ApplicationMode = ApplicationMode }; CloseAction?.Invoke(); }
}
