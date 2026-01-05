using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Transactions
{
    internal class MockTransactionRepository : ITransactionContext
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructor
        #endregion

        #region Methods
        public void Begin(string name)
        {
            MessageBox.Show($"Begin Transaction: {name}", "MockTransactionRepository");
        }

        public void Commit()
        {
            MessageBox.Show("Commit Transaction", "MockTransactionRepository");
        }

        public void Dispose()
        {
        }

        public void Rollback()
        {
            MessageBox.Show("Rollback Transaction", "MockTransactionRepository");
        }
        #endregion
    }
}
