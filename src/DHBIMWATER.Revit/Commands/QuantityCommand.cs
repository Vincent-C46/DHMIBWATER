using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DHBIMWATER.Revit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class QuantityCommand : EmbeddedDhBoostCommand
    {
        protected override string CommandTypeName => "DHBoost.Revit.Commands.Quantity.QuantityCommandForWater";
    }

    [Transaction(TransactionMode.Manual)]
    public class QuantitySettingCommand : EmbeddedDhBoostCommand
    {
        protected override string CommandTypeName => "DHBoost.Revit.Commands.Quantity.QuantitySettingCommandForWater";
    }

    public abstract class EmbeddedDhBoostCommand : IExternalCommand
    {
        protected abstract string CommandTypeName { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Assembly assembly = Assembly.Load("DHBoost.Revit");

                // DHBoost.Revit은 자체 Costura 패킹본이라 하위 어셈블리(DHBoost.UI 등)를 내부 리졸버로 푼다.
                // 바이트 배열로 로드된 어셈블리는 <Module> 정적 생성자가 자동 실행되지 않아 그 리졸버가
                // 등록되지 않으므로, 타입 해석 전에 모듈 초기화를 강제한다.
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

                // CreateInstance(string)은 의존성 로드 실패를 null로 삼키므로 throwOnError로 원인을 노출시킨다.
                Type commandType = assembly.GetType(CommandTypeName, throwOnError: true)!;
                if (Activator.CreateInstance(commandType) is not IExternalCommand command)
                {
                    message = $"내장된 DHBoost 명령을 만들 수 없습니다: {CommandTypeName}";
                    return Result.Failed;
                }

                return command.Execute(commandData, ref message, elements);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                message = ex.InnerException.Message;
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
