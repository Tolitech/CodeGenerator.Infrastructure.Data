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

        public DbTransaction Transaction { get; private set; } = null!;

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

        private void Audit(IAudit audit)
        {
            Task.Run(() =>
            {
                try
                {
                    _mediator.Send(new Auditing.Trail.Commands.InsertItems.InsertItemsCommand { Items = audit.Items });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        public void Commit()
        {
            if (_qtyTransactions == 1)
            {
                try
                {
                    Transaction.Commit();
                    
                    Audit(_audit);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

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

            try
            {
                foreach (var context in _contexts)
                {
                    context.Database.CloseConnection();

                    if (context.Database.CurrentTransaction != null)
                        context.Database.CurrentTransaction.Dispose();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
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