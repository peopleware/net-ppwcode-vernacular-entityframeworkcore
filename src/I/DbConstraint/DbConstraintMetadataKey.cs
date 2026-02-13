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

using System.Diagnostics;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.DbConstraint;

[DebuggerDisplay("{SchemaName}.{ConstraintName}")]
public class DbConstraintMetadataKey
    : IEquatable<DbConstraintMetadataKey>
{
    public DbConstraintMetadataKey(string schemaName, string constraintName)
    {
        SchemaName = schemaName;
        ConstraintName = constraintName;
    }

    public string SchemaName { get; }
    public string ConstraintName { get; }

    /// <inheritdoc />
    public bool Equals(DbConstraintMetadataKey? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return (SchemaName == other.SchemaName) && (ConstraintName == other.ConstraintName);
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

        return Equals((DbConstraintMetadataKey)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(SchemaName, ConstraintName);

    public static bool operator ==(DbConstraintMetadataKey? left, DbConstraintMetadataKey? right)
        => Equals(left, right);

    public static bool operator !=(DbConstraintMetadataKey? left, DbConstraintMetadataKey? right)
        => !Equals(left, right);
}
