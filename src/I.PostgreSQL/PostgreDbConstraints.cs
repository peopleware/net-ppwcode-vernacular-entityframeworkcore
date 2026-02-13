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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using PPWCode.Vernacular.EntityFrameworkCore.I.DbConstraint;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.PostgreSQL;

/// <inheritdoc cref="SchemaBasedDbConstraints" />
public class PostgreDbConstraints : SchemaBasedDbConstraints
{
    public PostgreDbConstraints(
        ILogger<PostgreDbConstraints> logger,
        IConfiguration configuration,
        string? connectionStringName = null)
        : base(logger, configuration, connectionStringName)
    {
    }

    /// <inheritdoc />
    protected override DbProviderFactory DbProviderFactory
        => NpgsqlFactory.Instance;

    /// <inheritdoc />
    protected override string CommandText
        => @"
select tc.constraint_name,
       tc.table_name,
       tc.table_schema,
       tc.constraint_type
  from information_schema.table_constraints tc
 where tc.constraint_catalog = @catalog
   and tc.constraint_schema not in ('pg_catalog', 'pg_toast', 'information_schema')
union all
select c.relname as constraint_name,
       t.relname as table_name,
       n.nspname as table_schema,
       'UNIQUE' as constraint_type
  from pg_catalog.pg_class c
       join pg_catalog.pg_namespace n on n.oid = c.relnamespace
       join pg_catalog.pg_index i on i.indexrelid = c.oid
       join pg_catalog.pg_class t on i.indrelid = t.oid
 where exists (
	     select *
	       from information_schema.table_constraints tc
	      where tc.constraint_catalog = @catalog
            and tc.constraint_schema not in ('pg_catalog', 'pg_toast', 'information_schema')
            and tc.table_schema = n.nspname
            and tc.table_name = t.relname)
   and not exists (
	     select *
	       from information_schema.table_constraints tc
	      where tc.constraint_catalog = @catalog
            and tc.constraint_schema not in ('pg_catalog', 'pg_toast', 'information_schema')
            and tc.table_schema = n.nspname
            and tc.constraint_name = c.relname
            and tc.constraint_type = 'UNIQUE')
   and c.relkind = 'i'
   and i.indisunique = true
   and i.indisprimary = false
";
}
