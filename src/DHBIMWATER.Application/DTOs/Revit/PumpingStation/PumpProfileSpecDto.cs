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
        string SelectedTheta,
        double L1,
        double L2,
        double L3,
        double L4,
        double H3,
        double H4,
        double H7,
        double OB1,
        double OH1,
        int NS,
        double HS
    );
}
