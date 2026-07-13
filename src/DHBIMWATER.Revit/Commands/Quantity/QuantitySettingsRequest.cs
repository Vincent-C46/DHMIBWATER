using System.Threading;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    public enum QuantitySettingsRequestId
    {
        None = 0,
        Open = 1,   // DataStorage에서 설정 로드 후 설정창 열기
        Save = 2,   // 현재 설정을 DataStorage에 저장
    }

    public class QuantitySettingsRequest
    {
        private int _requestId = (int)QuantitySettingsRequestId.None;

        public QuantitySettingsRequestId Take()
            => (QuantitySettingsRequestId)Interlocked.Exchange(ref _requestId, (int)QuantitySettingsRequestId.None);

        public void Make(QuantitySettingsRequestId requestId)
            => Interlocked.Exchange(ref _requestId, (int)requestId);
    }
}
