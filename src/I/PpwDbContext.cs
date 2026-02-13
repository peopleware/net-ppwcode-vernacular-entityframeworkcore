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

using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using PPWCode.Vernacular.Persistence.V;

namespace PPWCode.Vernacular.EntityFrameworkCore.I;

public abstract class PpwDbContext<TTimestamp> : DbContext
    where TTimestamp : struct, IComparable<TTimestamp>, IEquatable<TTimestamp>
{
    protected PpwDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected abstract IEnumerable<Assembly> ConfigurationAssemblies { get; }

    protected virtual int AuditColumnLength
        => 128;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (Assembly configurationAssembly in ConfigurationAssemblies)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(configurationAssembly);
        }

        // Be sure that the types are correct for audit record stamping
        foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        {
            List<Type> interfaces =
                entity
                    .ClrType.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .ToList();

            if (interfaces.Any(i => i.GetGenericTypeDefinition() == typeof(IInsertAuditable<>)))
            {
                IMutableProperty? createdByProperty = entity.FindProperty(nameof(IInsertAuditable<TTimestamp>.CreatedBy));
                createdByProperty?.SetMaxLength(AuditColumnLength);
            }

            if (interfaces.Any(i => i.GetGenericTypeDefinition() == typeof(IUpdateAuditable<>)))
            {
                IMutableProperty? createdByProperty = entity.FindProperty(nameof(IUpdateAuditable<TTimestamp>.LastModifiedBy));
                createdByProperty?.SetMaxLength(AuditColumnLength);
            }
        }
    }
}
