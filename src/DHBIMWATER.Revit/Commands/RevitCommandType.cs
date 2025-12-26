namespace DHBIMWATER.Revit.Commands
{
    public static class RevitCommandType<T>
    {
        public static readonly string FullName = typeof(T).FullName!;
    }
}
