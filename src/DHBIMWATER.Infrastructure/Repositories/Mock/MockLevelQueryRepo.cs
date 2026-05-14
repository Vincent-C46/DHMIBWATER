using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockLevelQueryRepo : ILevelQueryRepo
    {
        public IEnumerable<string> GetExistingLevelNames()
        {
            return new List<string> { "수조부 바닥슬래브", "Level 2", "Level 3" };
        }
        public IEnumerable<string> GetExistingPlanNames()
        {
            return new List<string> { "Plan A", "Plan B", "Plan C" };
        }
        public IEnumerable<string> GetExistingSectionNames()
        {
            return new List<string> { "Section A", "Section B", "Section C" };
        }
    }
}
