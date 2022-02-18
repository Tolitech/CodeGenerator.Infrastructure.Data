using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tolitech.CodeGenerator.Auditing;
using Tolitech.CodeGenerator.Domain.Repositories;
using Tolitech.CodeGenerator.Domain.Services;
using Tolitech.CodeGenerator.Infrastructure.Data.Transactions;

namespace Tolitech.CodeGenerator.Infrastructure.Data.Repositories
{
    public abstract class Repository : IRepository
    {
        private readonly DbContext _context;

        protected readonly IUnitOfWork _uow;
        protected readonly ILogger _logger;
        protected readonly IAudit _audit;

        public Repository(IUnitOfWorkService uow, DbContext context, ILogger logger, IAudit audit)
        {
            _uow = (IUnitOfWork)uow;

            _context = context;
            _logger = logger;
            _audit = audit;
        }

        public void UseTransaction()
        {
            if (_uow != null)
                _context.Database.UseTransaction(_uow.Transaction);
        }

        #region Logger

        public void LogRepositoryException(Exception ex, object? param)
        {
            const string template = "SqlParams: {parameters}";
            string? parameters = null;

            if (param != null)
                parameters = JsonSerializer.Serialize(param, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            
            _logger.LogWarning(ex, ex.Message + "\n" + template, parameters);
        }

        public void LogRepositoryException(Exception ex, string sql, object? param)
        {
            const string template = "Sql: {sql}" + "\n" + "SqlParams: {parameters}";
            string? parameters = null;

            if (param != null)
                parameters = JsonSerializer.Serialize(param, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            
            _logger.LogWarning(ex, ex.Message + "\n" + template, sql, parameters);
        }

        #endregion
    }
}