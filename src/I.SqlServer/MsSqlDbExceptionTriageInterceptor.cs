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

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using PPWCode.Vernacular.EntityFrameworkCore.I.DbConstraint;
using PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions;
using PPWCode.Vernacular.EntityFrameworkCore.I.Interceptors;

namespace PPWCode.Vernacular.EntityFrameworkCore.I.SqlServer;

public class MsSqlDbExceptionTriageInterceptor : DbExceptionTriageInterceptor<SqlException>
{
    private readonly IDbConstraints _dbConstraints;

    public MsSqlDbExceptionTriageInterceptor(IDbConstraints dbConstraints)
    {
        _dbConstraints = dbConstraints;
    }

    /// <inheritdoc />
    protected override void OnGatherExceptionData(
        Exception exception,
        SqlException providerException,
        DbConstraintExceptionDataBuilder dbConstraintExceptionDataBuilder,
        DbContext? eventContext)
    {
        if (providerException.Number == 515)
        {
            dbConstraintExceptionDataBuilder.ConstraintType(DbConstraintTypeEnum.NOT_NULL);
        }
        else if (providerException.Number is 8152 or 2628)
        {
            dbConstraintExceptionDataBuilder.ConstraintType(DbConstraintTypeEnum.DATA_TRUNCATED);
        }
        else
        {
            DbConstraintMetadata? metadata = null;
            List<DbConstraintMetadata> metadatas =
                _dbConstraints
                    .Constraints
                    .Where(c => providerException.Message.Contains(c.ConstraintName))
                    .ToList();
            if (metadatas.Count > 1)
            {
                metadatas =
                    metadatas
                        .Where(c => providerException.Message.Contains(c.FullQualifiedName))
                        .ToList();
            }

            if (metadatas.Count == 1)
            {
                metadata = metadatas.Single();
            }

            if (metadata is not null)
            {
                dbConstraintExceptionDataBuilder
                    .ConstraintType(metadata.ConstraintType)
                    .ConstraintName(metadata.ConstraintName)
                    .SchemaName(metadata.SchemaName)
                    .TableName(metadata.TableName);
            }
        }
    }
}
