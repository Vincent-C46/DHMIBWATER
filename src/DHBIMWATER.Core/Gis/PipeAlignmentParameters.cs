using DHBIMWATER.Core.Parameters;

namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 관로 모델링(Adaptive 직관·곡관) 결과물에 기록하는 정보용 프로젝트 매개변수 정의.
/// GUID는 docs/32_관로매개변수_지시서.md §2 표가 원본이며 변경하면 안 된다.
/// </summary>
public static class PipeAlignmentParameters
{
    public const string Addin = "DH_Addin";
    public const string Part = "DH_Part";
    public const string AlignmentId = "DH_관로ID";
    public const string NominalDiameter = "DH_호칭지름";
    public const string Material = "DH_관종";
    public const string Grade = "DH_관종등급";
    public const string OuterDiameter = "DH_외경";
    public const string WallThickness = "DH_관두께";
    public const string JointType = "DH_접합방법";
    public const string ElevationDatum = "DH_기준높이";
    public const string SourceFile = "DH_원본파일";
    public const string RecordNumber = "DH_레코드번호";
    public const string Length = "DH_연장";
    public const string StartElevation = "DH_시점표고";
    public const string EndElevation = "DH_종점표고";
    public const string Slope = "DH_경사";
    public const string NodeId = "DH_절점번호";
    public const string ActualDeflection = "DH_실제굴곡각";
    public const string StandardAngle = "DH_곡관규격각";
    public const string AllowableDeflection = "DH_허용굴곡각";
    public const string ResidualDeflection = "DH_잔여굴곡각";
    public const string IsAcceptable = "DH_허용여부";
    public const string JointApplication = "DH_접합적용";
    public const string CenterlineRadius = "DH_곡률반경";
    public const string BendFormName = "DH_곡관형식";
    public const string LayingLength = "DH_부설길이";
    public const string Weight = "DH_무게";

    public const string AddinValue = "DHBIMWATER";
    public const string StraightPartValue = "직관";
    public const string BendPartValue = "곡관";

    /// <summary>이름 → GUID. 하드코딩이며 변경 금지 (docs/32 §2).</summary>
    public static IReadOnlyDictionary<string, Guid> Guids { get; } = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        [Addin] = new("f0ff9795-a26f-4a2f-869c-532d2c418fac"), [Part] = new("47c74ae0-3fc8-4a06-9c4e-80713b564b0c"),
        [AlignmentId] = new("7f42986b-997a-4e96-ba69-6dcd6759cda5"), [NominalDiameter] = new("bdfa5b79-1ef4-416e-a7f7-ee98d93f92d4"),
        [Material] = new("7c34e9d9-bace-40bb-9924-3660e8db398f"), [Grade] = new("480d1033-9ed2-47c8-af8e-ab8ca7aed4f0"),
        [OuterDiameter] = new("16245952-f626-4226-8e77-8865d0678f61"), [WallThickness] = new("5b102026-7041-421b-9c95-2e7d61f981e0"),
        [JointType] = new("716a91a8-08b8-48b9-b6c1-4cdd98f1751d"), [ElevationDatum] = new("522778f9-6c5c-45e9-8f74-4aed525205bb"),
        [SourceFile] = new("86599c86-c79b-4f37-9bd9-82c5832faa60"), [RecordNumber] = new("8b11facb-f004-4706-a783-2ed792fc32da"),
        [Length] = new("0f8756f5-b4d2-48b6-9b9c-f112af0fbb08"), [StartElevation] = new("63dcca61-323a-44d8-aa44-c91564dd455c"),
        [EndElevation] = new("df8db332-3ce7-44fa-9f71-6e19a352de1a"), [Slope] = new("504500cb-d45e-4461-ad18-47a34168facc"),
        [NodeId] = new("bc52b57a-bb41-4f92-a9df-122842826d58"), [ActualDeflection] = new("93048122-1ee0-47df-add3-2851984ca7f3"),
        [StandardAngle] = new("3c2b2a81-35ed-48f9-bcaf-0307e36a163a"), [AllowableDeflection] = new("081b5774-0112-4aae-ba65-2d5bd26a9ccc"),
        [ResidualDeflection] = new("8f192977-1c4d-44df-b5d0-7aef46e1b120"), [IsAcceptable] = new("2d00a14e-573a-44ee-9c2d-4c9e60dc23dd"),
        [JointApplication] = new("7848d078-a439-4bba-ac20-68253e5b01be"), [CenterlineRadius] = new("dc55ce42-12ef-4416-b1c2-b3bf7e92c83f"),
        [BendFormName] = new("c1312dac-af34-464a-889c-a12fc906b1b0"), [LayingLength] = new("f9784b1d-8b57-45af-9221-868c51a08d6a"),
        [Weight] = new("a6e1f5b0-9c1e-4d7a-8e2f-1b3c4d5e6f70"),
    };

    public static IReadOnlyList<SharedParameterDefinition> Definitions { get; } = new[]
    {
        Text(Addin, ParameterGroupType.IdentityData, false), Text(Part, ParameterGroupType.IdentityData), Text(AlignmentId, ParameterGroupType.IdentityData),
        Text(NominalDiameter), Text(Material), Text(Grade), LengthDef(OuterDiameter), LengthDef(WallThickness), Text(JointType), Text(ElevationDatum), Text(SourceFile), Text(RecordNumber),
        LengthDef(Length), NumberDef(StartElevation), NumberDef(EndElevation), NumberDef(Slope), Def(NodeId, ParameterSpecType.Integer),
        Def(ActualDeflection, ParameterSpecType.Angle), Def(StandardAngle, ParameterSpecType.Angle), Def(AllowableDeflection, ParameterSpecType.Angle), Def(ResidualDeflection, ParameterSpecType.Angle),
        Def(IsAcceptable, ParameterSpecType.YesNo), Text(JointApplication), LengthDef(CenterlineRadius), Text(BendFormName), LengthDef(LayingLength),
        NumberDef(Weight),
    };

    public static string DiameterText(double diameterMm) => $"DN{diameterMm:0.##}";
    public static string Label(PipeMaterial material) => material switch { PipeMaterial.DuctileIron => "덕타일주철관", PipeMaterial.Steel => "강관", PipeMaterial.Pe => "PE관", PipeMaterial.Pvc => "PVC관", _ => material.ToString() };
    public static string Label(ZDatum datum) => datum switch { ZDatum.Centerline => "중심선", ZDatum.Invert => "관저고 (내부 바닥)", ZDatum.Crown => "크라운 (내부 천장)", ZDatum.OutsideTop => "외부 맨 위", ZDatum.OutsideBottom => "외부 맨 아래", _ => datum.ToString() };
    public static string Label(JointApplicationMode mode) => mode switch { JointApplicationMode.SingleJoint => "편측 1개소", JointApplicationMode.BothJoints => "양측 2개소", _ => mode.ToString() };
    public static string Label(BendForm form) => form == BendForm.AType ? "A형" : "B형";

    private static SharedParameterDefinition Text(string name, ParameterGroupType group = ParameterGroupType.Data, bool userModifiable = true) => Def(name, ParameterSpecType.Text, group, userModifiable);
    private static SharedParameterDefinition LengthDef(string name) => Def(name, ParameterSpecType.Length);
    private static SharedParameterDefinition NumberDef(string name) => Def(name, ParameterSpecType.Number);
    private static SharedParameterDefinition Def(string name, ParameterSpecType spec, ParameterGroupType group = ParameterGroupType.Data, bool userModifiable = true) => new() { Name = name, Guid = Guids[name], SpecType = spec, GroupType = group, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel }, UserModifiable = userModifiable };
}
