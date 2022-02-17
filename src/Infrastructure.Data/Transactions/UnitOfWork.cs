using System;
using System.Data;
using System.Data.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tolitech.CodeGenerator.Auditing;

namespace Tolitech.CodeGenerator.Infrastructure.Data.Transactions
{
    public abstract class UnitOfWork : IUnitOfWork, IDisposable
    {
        private bool _disposed;
        private int _qtyTransactions;

        private readonly IMediator _mediator;
        private readonly IAudit _audit;
        private readonly ICollection<DbContext> _contexts;

        public UnitOfWork(IMediator mediator, IAudit audit)
        {
            _mediator = mediator;
            _audit = audit;

            _qtyTransactions = 0;
            _contexts = new List<DbContext>();
            Connection = GetNewConnection();
        }

        public DbConnection Connection { get; private set; }

        public DbTransaction Transaction { get; private set; }

        public void AddContext(DbContext context)
        {
            if (!_contexts.Contains(context))
            {
                _contexts.Add(context);

                if (Transaction != null)
                {
                    if (context.Database.CurrentTransaction == null)
                    {
                        if (context.Database.GetDbConnection() == Transaction.Connection)
                            context.Database.UseTransaction(Transaction);
                    }
                }
            }
        }

        public abstract string GetConnectionString
        {
            get;
        }

        public abstract DbConnection GetNewConnection();

        public void BeginTransaction()
        {
            if (_qtyTransactions == 0)
            {
                if (Connection == null)
                    Connection = GetNewConnection();

                if (Connection.State == ConnectionState.Closed)
                {
                    if (string.IsNullOrEmpty(Connection.ConnectionString))
                        Connection.ConnectionString = GetConnectionString;

                    Connection.Open();
                }

                Transaction = Connection.BeginTransaction();

                foreach (var context in _contexts)
                {
                    if (context.Database.CurrentTransaction == null)
                        context.Database.UseTransaction(Transaction);
                }
            }

            _qtyTransactions++;
        }

        public abstract void Audit(IAudit audit);

        public void Commit()
        {
            if (_qtyTransactions == 1)
            {
                try
                {
                    Transaction.Commit();

                    Task.Run(() =>
                    {
                        Audit(_audit);
                    });
                }
                catch (Exception)
                {
                    Transaction.Rollback();
                    throw;
                }
                finally
                {
                    Close();
                }
            }

            _qtyTransactions--;
        }

        public void Rollback()
        {
            if (_qtyTransactions == 1)
            {
                try
                {
                    if (Transaction.Connection != null)
                        Transaction.Rollback();
                }
                finally
                {
                    Close();
                }
            }

            _qtyTransactions--;
        }

        private void Close()
        {
            Connection.Close();

            foreach (var context in _contexts)
            {
                context.Database.CloseConnection();

                if (context.Database.CurrentTransaction != null)
                    context.Database.CurrentTransaction.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Transaction != null)
                        Transaction.Dispose();

                    if (Connection != null)
                        Connection.Dispose();

                    foreach (var context in _contexts)
                        context.Dispose();

                    _contexts.Clear();
                }

                _disposed = true;
            }
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }
}