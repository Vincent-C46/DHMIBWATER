using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ITransactionContext : IDisposable
    {
        /// <summary>suppressWarnings가 true면 Revit 경고(Warning 등급)를 자동 삭제해 모달 대화상자 없이 진행한다(관로 모델링 대량 배치 전용, 2026-08-26).</summary>
        void Begin(string name, bool suppressWarnings = false);
        void Commit();
        void Rollback();
        /// <summary>Begin(suppressWarnings: true)로 억제된 경고 설명 목록. suppressWarnings를 안 썼거나 아직 없으면 빈 목록.</summary>
        IReadOnlyList<string> SuppressedWarnings { get; }
    }
}
