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
using Microsoft.EntityFrameworkCore.Diagnostics;

using PPWCode.Vernacular.Exceptions.V;
using PPWCode.Vernacular.Semantics.V;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Interceptors;

public class WildExceptionsInterceptor : SaveChangesInterceptor
{
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

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => SavingChangesAsync(eventData, result)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    private CompoundSemanticException Validate(DbContext dbContext)
    {
        CompoundSemanticException cse = new();

        IEnumerable<ICivilizedObject> civilizedObjects =
            dbContext
                .ChangeTracker
                .Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .Select(e => e.Entity)
                .OfType<ICivilizedObject>();
        foreach (ICivilizedObject civilizedObject in civilizedObjects)
        {
            cse.AddElement(civilizedObject.WildExceptions());
        }

        return cse;
    }
}
