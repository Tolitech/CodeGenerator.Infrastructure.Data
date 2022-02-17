using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Tolitech.CodeGenerator.Domain.Services;
using Tolitech.CodeGenerator.Infrastructure.Data.Transactions;

namespace Tolitech.CodeGenerator.Infrastructure.Data.Contexts
{
    public abstract class DatabaseContext : DbContext
    {
        protected readonly DbConnection? _conn;

        public DatabaseContext(IUnitOfWorkService unitOfWork)
        {
            if (unitOfWork is IUnitOfWork uow)
            {
                _conn = uow.Connection;
                uow.AddContext(this);
            }

            ChangeTrackerConfiguration();
        }

        private void ChangeTrackerConfiguration()
        {
            ChangeTracker.AutoDetectChangesEnabled = false;
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.Context.Database.AutoTransactionsEnabled = false;
            ChangeTracker.LazyLoadingEnabled = false;
        }
    }
}