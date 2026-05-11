using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System;

namespace DHBIMWATER.Infrastructure.Transactions

{
    internal class RevitTransactionContext : ITransactionContext
    {
        #region Fields
        private readonly Func<Document?> _doc;
        private Transaction? _tx;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public RevitTransactionContext(Func<Document?> doc)
        {
            _doc = doc;
        }
        #endregion

        #region Properties
        public void Begin(string name)
        {
            var doc = _doc();
            if (doc == null)
                throw new InvalidOperationException("Documnet가 null입니다.");

            if(_tx != null) throw new InvalidOperationException("이미 트랜잭션이 시작되었습니다.");

            _tx = new Transaction(doc, name);
            _tx.Start();
        }

        public void Commit()
        {
            if (_tx == null)
                throw new InvalidOperationException("트랜잭션이 시작되지 않았습니다.");

            _tx.Commit();
            _tx.Dispose();
        }

        public void Rollback()
        {
            if(_tx != null && _tx.HasStarted())
            {
                _tx.RollBack();
            }
            Dispose();
        }

        public void Dispose()
        {
            if (_tx != null)
            {
                _tx.Dispose();
                _tx = null;
            }
        }
        #endregion
    }
}
