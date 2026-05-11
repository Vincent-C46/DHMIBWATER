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
    internal class MockTransactionContext : ITransactionContext
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
            MessageBox.Show($"트랜잭션 시작: {name}", "MockTransactionContext");
        }

        public void Commit()
        {
            MessageBox.Show("트랜잭션 커밋", "MockTransactionContext");
        }

        public void Dispose()
        {
        }

        public void Rollback()
        {
            MessageBox.Show("트랜잭션 롤백", "MockTransactionContext");
        }
        #endregion
    }
}
