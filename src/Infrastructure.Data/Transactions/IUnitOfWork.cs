using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Tolitech.CodeGenerator.Infrastructure.Data.Transactions
{
    public interface IUnitOfWork : Domain.Services.IUnitOfWorkService
    {
        DbConnection Connection { get; }

        DbTransaction Transaction { get; }

        string GetConnectionString { get; }

        DbConnection GetNewConnection();

        void AddContext(DbContext context);
    }
}
