using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockElementTypeQueryRepo : IElementTypeQueryRepo
    {
        public IEnumerable<string> GetBeamTypeNames()
        {
            return new List<string>
            {
                "Mock 보 타입 1 - 300x600",
                "Mock 보 타입 2 - 400x700",
                "Mock 보 타입 3 - 500x800",
                "Mock 보 타입 4 - 600x900"
            };
        }
        public IEnumerable<string> GetAdaptiveComponentTypeNames() => new[] { "Mock 5점 곡관 : DN100", "Mock 5점 곡관 : DN200", "Mock 3점 곡관 : DN300" };
        public int GetAdaptiveBendPointCount(string familyTypeName) => familyTypeName.StartsWith("Mock 5점 곡관", StringComparison.Ordinal) ? 5 : familyTypeName.StartsWith("Mock 3점 곡관", StringComparison.Ordinal) ? 3 : -1;

        public IEnumerable<string> GetColumnTypeNames()
        {
            return new List<string>
            {
                "Mock 기둥 타입 1 - 400x400",
                "Mock 기둥 타입 2 - 500x500",
                "Mock 기둥 타입 3 - 600x600",
                "Mock 기둥 타입 4 - 700x700"
            };
        }
        public IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName) => new[] { "직경", "관종", "주석", "설명" };
        public IEnumerable<string> GetAdaptiveInstanceParameterNames(string familyTypeName) => new[] { "DN", "관종", "주석", "설명" };
        public IEnumerable<string> GetPipingSystemTypeNames() => new[] { "Mock 파이프 시스템 1 - 급수" };
        public IEnumerable<string> GetPipeTypeNames() => new[] { "Mock PipeType 1" };
        public IEnumerable<string> GetPipeAccessoryTypeNames() => new[] { "Mock 밸브 : DN100", "Mock 밸브 : DN200", "Mock 플랜지 : DN100" };
        public IEnumerable<string> GetGenericModelTypeNames() => new[] { "Mock 일반모델 밸브 : DN100", "Mock 일반모델 밸브 : DN200" };

        public IEnumerable<string> GetFoundationTypeNames()
        {
            return new List<string>
            {
                "Mock 기초 타입 1 - THK200",
                "Mock 기초 타입 2 - THK250",
                "Mock 기초 타입 3 - THK300",
                "Mock 기초 타입 4 - THK350"
            };
        }

        public IEnumerable<string> GetSlabTypeNames()
        {
            return new List<string>
            {
                "Mock 슬래브 타입 1 - THK150",
                "Mock 슬래브 타입 2 - THK180",
                "Mock 슬래브 타입 3 - THK200",
                "Mock 슬래브 타입 4 - THK250",
                "Mock 슬래브 타입 5 - THK300"
            };
        }

        public IEnumerable<string> GetWallTypeNames()
        {
            return new List<string>
            {
                "Mock 벽체 타입 1 - THK200",
                "Mock 벽체 타입 2 - THK250",
                "Mock 벽체 타입 3 - THK300",
                "Mock 벽체 타입 4 - THK350"
            };
        }
    }
}
