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

using PPWCode.Vernacular.Contracts.I;
using PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.DbConstraint
{
    public class DbConstraintMetadataBuilder
    {
        private string? _constraintName;
        private DbConstraintTypeEnum? _dbConstraintType;
        private string? _schemaName;
        private string? _tableName;

        public DbConstraintMetadataBuilder(DbConstraintMetadata? dbConstraintMetadata = null)
        {
            Merge(dbConstraintMetadata);
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder Merge(DbConstraintMetadata? dbConstraintMetadata)
        {
            if (dbConstraintMetadata is not null)
            {
                ConstraintName(dbConstraintMetadata.ConstraintName);
                TableName(dbConstraintMetadata.TableName);
                SchemaName(dbConstraintMetadata.SchemaName);
                DbConstraintType(dbConstraintMetadata.ConstraintType);
            }

            return this;
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder ConstraintName(string constraintName)
        {
            _constraintName = constraintName;
            return this;
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder TableName(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder SchemaName(string tableSchema)
        {
            _schemaName = tableSchema;
            return this;
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder DbConstraintType(DbConstraintTypeEnum dbConstraintType)
        {
            _dbConstraintType = dbConstraintType;
            return this;
        }

        [DebuggerStepThrough]
        public DbConstraintMetadataBuilder DbConstraintType(string constraintType)
        {
            switch (constraintType)
            {
                case "PRIMARY KEY":
                    _dbConstraintType = DbConstraintTypeEnum.PRIMARY_KEY;
                    break;

                case "UNIQUE":
                    _dbConstraintType = DbConstraintTypeEnum.UNIQUE;
                    break;

                case "FOREIGN KEY":
                    _dbConstraintType = DbConstraintTypeEnum.FOREIGN_KEY;
                    break;

                case "CHECK":
                    _dbConstraintType = DbConstraintTypeEnum.CHECK;
                    break;
            }

            return this;
        }

        public DbConstraintMetadata Build()
            => this;

        public static implicit operator DbConstraintMetadata(DbConstraintMetadataBuilder builder)
        {
            Contract.Assert(builder._constraintName is not null);
            Contract.Assert(builder._schemaName is not null);
            Contract.Assert(builder._tableName is not null);

            return
                new DbConstraintMetadata(
                    builder._constraintName,
                    builder._dbConstraintType ?? DbConstraintTypeEnum.PRIMARY_KEY,
                    builder._schemaName,
                    builder._tableName);
        }
    }
}
