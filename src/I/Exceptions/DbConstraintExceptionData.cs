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

using System.Text;

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;

public class DbConstraintExceptionData
    : IEquatable<DbConstraintExceptionData>
{
    public DbConstraintExceptionData(
        DbConstraintTypeEnum? constraintType,
        string? constraintName,
        string? schemaName,
        string? tableName,
        IEnumerable<EntityEntry> entries,
        string? entityName = null,
        object? entityKey = null)
    {
        ConstraintType = constraintType;
        ConstraintName = constraintName;
        SchemaName = schemaName;
        TableName = tableName;
        EntityName = entityName;
        EntityKey = entityKey;
        Entries = new List<EntityEntry>(entries).AsReadOnly();
    }

    public DbConstraintTypeEnum? ConstraintType { get; }
    public string? ConstraintName { get; }

    public string? SchemaName { get; }
    public string? TableName { get; }

    public string? EntityName { get; }
    public object? EntityKey { get; }

    public IReadOnlyList<EntityEntry> Entries { get; }

    /// <inheritdoc />
    public bool Equals(DbConstraintExceptionData? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return
            (ConstraintType == other.ConstraintType)
            && Equals(EntityKey, other.EntityKey)
            && (ConstraintName == other.ConstraintName)
            && (EntityName == other.EntityName);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((DbConstraintExceptionData)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(ConstraintName, EntityKey, EntityName);

    public static bool operator ==(DbConstraintExceptionData? left, DbConstraintExceptionData? right)
        => Equals(left, right);

    public static bool operator !=(DbConstraintExceptionData? left, DbConstraintExceptionData? right)
        => !Equals(left, right);

    public override string ToString()
        => new StringBuilder()
            .AppendFormat("{0}: {1}", nameof(ConstraintType), ConstraintType).AppendLine()
            .AppendFormat("{0}: {1}", nameof(ConstraintName), ConstraintName).AppendLine()
            .AppendFormat("{0}: {1}", nameof(SchemaName), SchemaName).AppendLine()
            .AppendFormat("{0}: {1}", nameof(TableName), TableName).AppendLine()
            .AppendFormat("{0}: {1}", nameof(EntityName), EntityName).AppendLine()
            .AppendFormat("{0}: {1}", nameof(EntityKey), EntityKey).AppendLine()
            .ToString();
}
