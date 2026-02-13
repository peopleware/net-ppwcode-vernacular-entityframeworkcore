// Copyright 2025 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

using PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;

using DbException = System.Data.Common.DbException;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Interceptors;

public abstract class DbExceptionTriageInterceptor<TProviderException>
    : IDbCommandInterceptor,
      ISaveChangesInterceptor
    where TProviderException : DbException
{
    /// <inheritdoc />
    public void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        => ProcessException(eventData.Exception, eventData.Context);

    /// <inheritdoc />
    public Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ProcessException(eventData.Exception, eventData.Context);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SaveChangesFailed(DbContextErrorEventData eventData)
        => ProcessException(eventData.Exception, eventData.Context);

    /// <inheritdoc />
    public Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        ProcessException(eventData.Exception, eventData.Context);
        return Task.CompletedTask;
    }

    [StackTraceHidden]
    protected virtual void ProcessException(Exception eventException, DbContext? eventContext)
    {
        TProviderException? providerException = ExtractProviderException(eventException);
        if (providerException is not null)
        {
            DbConstraintExceptionDataBuilder builder = new();

            if (eventException is DbUpdateException dbUpdateException)
            {
                IReadOnlyList<EntityEntry> entries = dbUpdateException.Entries;
                builder.Entries(entries);

                EntityEntry? entityEntry = entries.FirstOrDefault();
                if (entityEntry is not null)
                {
                    IEntityType entityMetadata = entityEntry.Metadata;
                    builder.EntityName(entityMetadata.ClrType.FullName);
                    if (entityEntry.IsKeySet)
                    {
                        IProperty? firstKeyProperty =
                            entityMetadata
                                .FindPrimaryKey()
                                ?.Properties
                                .FirstOrDefault();
                        if (firstKeyProperty is not null)
                        {
                            builder.EntityKey(entityEntry.Property(firstKeyProperty).CurrentValue);
                        }
                    }
                }
            }

            OnGatherExceptionData(eventException, providerException, builder, eventContext);
            DbConstraintExceptionData dbConstraintExceptionData = builder.Build();
            if (dbConstraintExceptionData.ConstraintType is not null)
            {
                ThrowException(dbConstraintExceptionData, providerException);
            }
        }
    }

    [DoesNotReturn]
    public virtual void ThrowException(
        DbConstraintExceptionData dbConstraintExceptionData,
        TProviderException providerException)
    {
        string message = providerException.Message;
        Exception? innerException = providerException.InnerException;

        switch (dbConstraintExceptionData.ConstraintType)
        {
            case DbConstraintTypeEnum.PRIMARY_KEY:
                throw new DbPrimaryKeyConstraintException(message, innerException, dbConstraintExceptionData);

            case DbConstraintTypeEnum.UNIQUE:
                throw new DbUniqueConstraintException(message, innerException, dbConstraintExceptionData);

            case DbConstraintTypeEnum.FOREIGN_KEY:
                throw new DbForeignKeyConstraintException(message, innerException, dbConstraintExceptionData);

            case DbConstraintTypeEnum.CHECK:
                throw new DbCheckConstraintException(message, innerException, dbConstraintExceptionData);

            case DbConstraintTypeEnum.NOT_NULL:
                throw new DbNotNullConstraintException(message, innerException, dbConstraintExceptionData);

            case DbConstraintTypeEnum.DATA_TRUNCATED:
                throw new DbDataTruncatedConstraintException(message, innerException, dbConstraintExceptionData);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected abstract void OnGatherExceptionData(
        Exception exception,
        TProviderException providerException,
        DbConstraintExceptionDataBuilder dbConstraintExceptionDataBuilder,
        DbContext? eventContext);

    protected virtual TProviderException? ExtractProviderException(Exception sqlException)
    {
        Exception? baseException = sqlException;
        TProviderException? result = sqlException as TProviderException;
        while ((result == null) && (baseException != null))
        {
            baseException = baseException.InnerException;
            result = baseException as TProviderException;
        }

        return result;
    }
}
