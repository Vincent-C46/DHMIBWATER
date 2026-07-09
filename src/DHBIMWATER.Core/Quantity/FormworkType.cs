namespace DHBIMWATER.Core.Quantity
{
    public enum FormworkType
    {
        Euroform,           // 유로폼
        Plywood3,           // 합판3회
        Plywood4,           // 합판4회
        Plywood6,           // 합판6회
        AlForm,             // 알루미늄 폼
        GangForm,           // 갱폼

    }

    public static class FormworkTypeExtensions
    {
        public static string ToSpecification(this FormworkType type) => type switch
        {
            FormworkType.Euroform => "유로폼",
            FormworkType.Plywood3 => "합판3회",
            FormworkType.Plywood4 => "합판4회",
            FormworkType.Plywood6 => "합판6회",
            FormworkType.AlForm => "알폼",
            FormworkType.GangForm => "갱폼",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}
