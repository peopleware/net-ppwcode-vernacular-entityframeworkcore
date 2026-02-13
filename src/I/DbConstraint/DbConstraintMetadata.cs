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

using PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.DbConstraint
{
    public class DbConstraintMetadata
        : IEquatable<DbConstraintMetadata>
    {
        public DbConstraintMetadata(
            string constraintName,
            DbConstraintTypeEnum constraintType,
            string schemaName,
            string tableName)
        {
            ConstraintName = constraintName;
            ConstraintType = constraintType;
            SchemaName = schemaName;
            TableName = tableName;
        }

        public string ConstraintName { get; }
        public DbConstraintTypeEnum ConstraintType { get; }
        public string TableName { get; }
        public string SchemaName { get; }

        public DbConstraintMetadataKey Key
            => new(SchemaName, ConstraintName);

        public string FullQualifiedName
            => $"{SchemaName}.{ConstraintName}";

        /// <inheritdoc />
        public bool Equals(DbConstraintMetadata? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Key.Equals(other.Key);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
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

            return Equals((DbConstraintMetadata)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
            => Key.GetHashCode();

        public static bool operator ==(DbConstraintMetadata left, DbConstraintMetadata right)
            => Equals(left, right);

        public static bool operator !=(DbConstraintMetadata left, DbConstraintMetadata right)
            => !Equals(left, right);
    }
}
