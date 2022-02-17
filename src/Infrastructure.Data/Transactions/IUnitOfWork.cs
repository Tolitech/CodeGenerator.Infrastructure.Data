using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Tolitech.CodeGenerator.Auditing;

namespace Tolitech.CodeGenerator.Infrastructure.Data.Transactions
{
    public interface IUnitOfWork
    {
        DbConnection Connection { get; }

        DbTransaction Transaction { get; }

        string GetConnectionString { get; }

        DbConnection GetNewConnection();

        void AddContext(DbContext context);

        void Audit(IAudit audit);
    }
}
