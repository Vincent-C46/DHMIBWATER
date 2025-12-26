using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;

namespace DHBIMWATER.Revit.Commands
{
    public abstract class CommandBase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 공통 서비스 초기화
            ServiceContainer.Build(commandData.Application);

            try
            {
                return ExecuteInternal(commandData, ref message, elements);
            }
            catch (System.Exception ex)
            {
                // 예외 처리 로직
                message = ex.Message;
                return Result.Failed;
            }
            finally
            {
                // 서비스 정리
                ServiceContainer.Dispose();
            }
        }

        protected abstract Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements);
    }
}
