using DHBIMWATER.Application.Interfaces;
using DidasUsage;
using System.Diagnostics;

namespace DHBIMWATER.Infrastructure.Services.Didas;

public class DidasUsageService : IUsageLogger
{
    private const string AppType = "BIM";
    private const string Pgm = "00000000000198562291";
    private const string EvtCd = "EVTBIM002";
    private const string EvtRefNm = "DHBIMWATER";

    public async Task LogAsync()
    {
        var result = await DidasUsageTracker.SendAsync(AppType, Pgm, EvtCd, EvtRefNm);
        var userInfo = DidasUsageTracker.GetUserInfo();

        Debug.WriteLine($"evtRefNm={EvtRefNm} | errorCode={result.ErrorCode} | errorMsg={result.ErrorMsg}");
        Debug.WriteLine($"keyCode={userInfo.KeyCode} |  userName={userInfo.UserName} | partName={userInfo.PartName}");
    }
}
