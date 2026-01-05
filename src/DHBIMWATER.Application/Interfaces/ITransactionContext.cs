using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ITransactionContext : IDisposable
    {
        void Begin(string name);
        void Commit();
        void Rollback();
    }
}