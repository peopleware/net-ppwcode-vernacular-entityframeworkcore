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

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;

public class DbConstraintExceptionDataBuilder
{
    private readonly List<EntityEntry> _entries = new();
    private string? _constraintName;
    private DbConstraintTypeEnum? _constraintType;
    private object? _entityKey;
    private string? _entityName;
    private string? _schemaName;
    private string? _tableName;

    public DbConstraintExceptionDataBuilder(DbConstraintExceptionData? dbConstraintExceptionData = null)
    {
        Merge(dbConstraintExceptionData);
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder Merge(DbConstraintExceptionData? dbConstraintExceptionData)
    {
        if (dbConstraintExceptionData is not null)
        {
            ConstraintType(dbConstraintExceptionData.ConstraintType);
            ConstraintName(dbConstraintExceptionData.ConstraintName);
            SchemaName(dbConstraintExceptionData.SchemaName);
            TableName(dbConstraintExceptionData.TableName);
            EntityName(dbConstraintExceptionData.EntityName);
            EntityKey(dbConstraintExceptionData.EntityKey);
            Entries(dbConstraintExceptionData.Entries);
        }

        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder ConstraintType(DbConstraintTypeEnum? constraintType)
    {
        _constraintType = constraintType;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder ConstraintName(string? constraintName)
    {
        _constraintName = constraintName;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder SchemaName(string? schemaName)
    {
        _schemaName = schemaName;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder TableName(string? tableName)
    {
        _tableName = tableName;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder EntityName(string? entityName)
    {
        _entityName = entityName;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder EntityKey(object? entityKey)
    {
        _entityKey = entityKey;
        return this;
    }

    [DebuggerStepThrough]
    public DbConstraintExceptionDataBuilder Entries(IEnumerable<EntityEntry> entries)
    {
        if (entries.Any())
        {
            _entries.AddRange(entries);
        }
        else
        {
            _entries.Clear();
        }

        return this;
    }

    public DbConstraintExceptionData Build()
        => this;

    public static implicit operator DbConstraintExceptionData(DbConstraintExceptionDataBuilder builder)
        => new(
            builder._constraintType,
            builder._constraintName,
            builder._schemaName,
            builder._tableName,
            builder._entries,
            builder._entityName,
            builder._entityKey);
}
