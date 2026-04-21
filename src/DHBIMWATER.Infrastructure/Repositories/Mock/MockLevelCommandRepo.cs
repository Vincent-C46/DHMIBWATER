using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockLevelCommandRepo : ILevelCommandRepo
    {
        public int CreateLevel(string levelName, double elevation)
        {
            MessageBox.Show($"MockLevelCommandRepo - CreateLevel\nLevel Name: {levelName} \nElevation: {elevation}m");
            return 0;
        }

        public int UpdateLevel(string levelName, double elevation)
        {
            MessageBox.Show($"MockLevelCommandRepo - UpdateLevel\nLevel Name: {levelName} \nNew Elevation: {elevation}m");
            return 0;
        }
    }
}
