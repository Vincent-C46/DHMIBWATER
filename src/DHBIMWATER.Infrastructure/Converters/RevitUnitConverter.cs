using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Converters
{
    public class RevitUnitConverter
    {
        // 길이
        public static double MmToFt(double millimeters) => UnitUtils.ConvertToInternalUnits(millimeters, UnitTypeId.Millimeters);
        public static double FtToMm(double feet) => UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters);
        public static double MToFt(double meters) => UnitUtils.ConvertToInternalUnits(meters, UnitTypeId.Meters);
        public static double FtToM(double feet) => UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Meters);

        // 면적
        public static double Ft2ToM2(double squareFeet) => UnitUtils.ConvertFromInternalUnits(squareFeet, UnitTypeId.SquareMeters);
        public static double M2ToFt2(double squareMeters) => UnitUtils.ConvertToInternalUnits(squareMeters, UnitTypeId.SquareMeters);

        // 체적
        public static double Ft3ToM3(double cubicFeet) => UnitUtils.ConvertFromInternalUnits(cubicFeet, UnitTypeId.CubicMeters);
        public static double M3ToFt3(double cubicMeters) => UnitUtils.ConvertToInternalUnits(cubicMeters, UnitTypeId.CubicMeters);
    }
}
