using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IViewCommandRepo
    {
        int CreateSectionView(SectionViewDefinition sectionViewDef);
        //int UpdateSectionView(SectionViewDefinition sectionViewDef);
    }
}
