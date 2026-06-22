namespace DHBIMWATER.Core.Quantity
{
    // Revit BuiltInCategory 정수값을 Core에서 참조 가능하도록 미러링
    // Infrastructure에서 (int)BuiltInCategory.OST_X 와 값이 일치해야 함
    public enum RevitCategory
    {
        Walls                = -2000011,
        Floors               = -2000032,
        StructuralColumns    = -2001330,
        StructuralFraming    = -2001331,
        StructuralFoundation = -2001329,
        GenericModel         = -2000151,
        Railings             = -2000126,
        Stairs               = -2000120,
    }
}
