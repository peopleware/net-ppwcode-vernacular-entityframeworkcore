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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

using PPWCode.Vernacular.Exceptions.V;
using PPWCode.Vernacular.Persistence.V;
using PPWCode.Vernacular.RequestContext.I;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Interceptors;

/// <summary>
///     Provides an entity framework interceptor to automatically populate auditing metadata on entities
///     implementing <see cref="IInsertAuditable{TTimestamp}" /> and <see cref="IUpdateAuditable{TTimestamp}" />.
/// </summary>
/// <typeparam name="TTimestamp">
///     The value type used for timestamps, typically <see cref="DateTime" /> or <see cref="DateTimeOffset" />.
/// </typeparam>
public abstract class AuditableInterceptor<TTimestamp> : SaveChangesInterceptor
    where TTimestamp : struct, IComparable<TTimestamp>, IEquatable<TTimestamp>
{
    private readonly IRequestContext<TTimestamp> _requestContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuditableInterceptor{TTimestamp}" /> class.
    /// </summary>
    /// <param name="requestContext">The context providing current request metadata such as identity and timestamp.</param>
    protected AuditableInterceptor(IRequestContext<TTimestamp> requestContext)
    {
        _requestContext = requestContext;
    }

    /// <summary>
    ///     Intercepts the asynchronous saving process to apply auditing logic before data is committed to the database.
    /// </summary>
    /// <param name="eventData">Contextual data regarding the DbContext event.</param>
    /// <param name="result">The current interception result.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    /// <exception cref="ProgrammingError">Thrown when the <see cref="DbContext" /> is null.</exception>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }
        else
        {
            throw new ProgrammingError("Expected context to be initialized.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     This synchronous implementation wraps the asynchronous call.
    ///     Note: It is recommended to use asynchronous saving in modern applications.
    /// </remarks>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => SavingChangesAsync(eventData, result)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     Iterates through the <see cref="ChangeTracker" /> to update auditing properties based on the state of the entities.
    /// </summary>
    /// <param name="context">The current <see cref="DbContext" /> instance.</param>
    /// <remarks>
    ///     Entities in the <see cref="EntityState.Added" /> state get their creation metadata set.
    ///     Entities in the <see cref="EntityState.Modified" /> state get their last modification metadata updated.
    /// </remarks>
    protected virtual void UpdateAuditableEntities(DbContext context)
    {
        object requestTimestamp = _requestContext.RequestTimestamp;
        object identityName = _requestContext.IdentityName;

        // Populate creation audit fields for new entities
        foreach (EntityEntry<IInsertAuditable<TTimestamp>> entityEntry in
                 context
                     .ChangeTracker
                     .Entries<IInsertAuditable<TTimestamp>>()
                     .Where(e => e.State == EntityState.Added))
        {
            entityEntry.Property(nameof(IInsertAuditable<TTimestamp>.CreatedAt)).CurrentValue = requestTimestamp;
            entityEntry.Property(nameof(IInsertAuditable<TTimestamp>.CreatedBy)).CurrentValue = identityName;
        }

        // Populate modification audit fields for updated entities
        foreach (EntityEntry<IUpdateAuditable<TTimestamp>> entityEntry in
                 context
                     .ChangeTracker
                     .Entries<IUpdateAuditable<TTimestamp>>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entityEntry.Property(nameof(IUpdateAuditable<TTimestamp>.LastModifiedAt)).CurrentValue = requestTimestamp;
            entityEntry.Property(nameof(IUpdateAuditable<TTimestamp>.LastModifiedBy)).CurrentValue = identityName;
        }
    }
}
