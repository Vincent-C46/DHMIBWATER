using DHBIMWATER.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public class ReservoirDto
    {
        //public Point3DDto StartPt { get; set; }
        //public Point3DDto EndPt { get; set; }

        // 설계조건
        public double Q { get; set; }
        public double RT{ get; set; }
        public int N{ get; set; }
        public double LWL { get; set; }

        // 수조부
        public double He { get; set; }
        public double Hf { get; set; }
        public double Hm { get; set; }
        public double W { get; set; }
        public double L { get; set; }
        public double M1 { get; set; }
        public double M2 { get; set; }
        public double M3 { get; set; }
        public double M4 { get; set; }
        public double Wh { get; set; }
        public double Lh { get; set; }
        public double Hh { get; set; }
        public double Lt { get; set; }

        // 밸브실
        public double H1 { get; set; }
        public double H2 { get; set; }
        public double Lv { get; set; }
        public double Wv { get; set; }

        // Con'c 단면선택
        public string TankUpperSlabName{get; set;}
        public string TankFndSlabName{get; set;}
        public string TankOuterWallName{get; set;}
        public string TankInnerWallName{get; set;}
        public string TankColumnName{get; set;}
        public string TankBeamName{get; set;}
        public string ValveUpperSlabName{get; set;}
        public string ValveMidSlabName{get; set;}
        public string ValveFndSlabName{get; set;}
        public string ValveOuterWallName { get; set; }
        public string SubFndSlabName { get; set; }

        //public List<ReservoirWallDto> Walls { get; set; } = new();
    }
}
