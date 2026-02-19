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
using PPWCode.Vernacular.Semantics.V;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Interceptors;

/// <summary>
///     An Entity Framework interceptor that performs domain-level validation before any changes are saved to the database.
///     It ensures that all entities implementing <see cref="ICivilizedObject" /> pass their internal validation rules.
/// </summary>
public class WildExceptionsInterceptor : SaveChangesInterceptor
{
    /// <summary>
    ///     Intercepts the asynchronous save process to execute domain validation.
    /// </summary>
    /// <param name="eventData">Contextual information about the current <see cref="DbContext" /> operation.</param>
    /// <param name="result">The current result of the interception.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the interception result.</returns>
    /// <exception cref="CompoundSemanticException">Thrown when one or more entities fail their validation checks.</exception>
    /// <exception cref="ProgrammingError">Thrown if the <see cref="DbContext" /> is not properly initialized.</exception>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            CompoundSemanticException cse = Validate(eventData.Context);
            if (!cse.IsEmpty)
            {
                throw cse;
            }
        }
        else
        {
            throw new ProgrammingError("Expected context to be initialized.");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     This method synchronously executes the asynchronous validation logic.
    ///     Usage of <c>SaveChangesAsync</c> is generally preferred in modern applications.
    /// </remarks>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => SavingChangesAsync(eventData, result)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     Scans the <see cref="ChangeTracker" /> for added or modified entities that implement
    ///     <see cref="ICivilizedObject" />
    ///     and aggregates any validation errors they report.
    /// </summary>
    /// <param name="dbContext">The current database context containing the tracked changes.</param>
    /// <returns>A <see cref="CompoundSemanticException" /> containing all gathered validation errors.</returns>
    /// <remarks>
    ///     Only entities in the <see cref="EntityState.Added" /> or <see cref="EntityState.Modified" /> states are validated,
    ///     as these represent the data currently being pushed to the store.
    /// </remarks>
    protected virtual CompoundSemanticException Validate(DbContext dbContext)
    {
        CompoundSemanticException cse = new();

        // Identify objects that require domain-rule checking
        IEnumerable<ICivilizedObject> civilizedObjects =
            dbContext
                .ChangeTracker
                .Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity)
                .OfType<ICivilizedObject>();

        foreach (ICivilizedObject civilizedObject in civilizedObjects)
        {
            // Gather "wild" exceptions from each object and add them to the compound exception
            cse.AddElement(civilizedObject.WildExceptions());
        }

        return cse;
    }
}
