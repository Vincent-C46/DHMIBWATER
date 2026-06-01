using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockViewCommandRepo : IViewCommandRepo
    {
        public int CreateSectionView(SectionViewDefinition sectionViewDef)
        {
            return 0;
        }

        //public int UpdateSectionView(SectionViewDefinition sectionViewDef)
        //{
        //    return 0;
        //}
    }
}
