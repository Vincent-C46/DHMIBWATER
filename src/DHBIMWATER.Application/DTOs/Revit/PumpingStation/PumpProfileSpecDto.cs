using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpProfileSpecDto
    (
        double B1,
        double B3,
        double B4,
        double B6,
        double B7,
        double H1,
        double H6,
        string Theta,
        double L1,
        double L2,
        double L3,
        double L4,
        double H3,
        double H4,
        double H7,
        double Ob1,
        double Oh1,
        int Ns,
        double Hs
    );
}
