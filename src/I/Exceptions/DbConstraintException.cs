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

namespace PPWCode.Vernacular.EntityFrameworkCore.I.Exceptions
{
    [Serializable]
    public abstract class DbConstraintException : DbUpdateException
    {
        private const string DbConstraintDataKey = "DbConstraintException.DbConstraintExceptionData";

        protected DbConstraintException(
            string message,
            Exception? innerException,
            DbConstraintExceptionData constraintExceptionData)
            : base(message, innerException, constraintExceptionData.Entries)
        {
            DbConstraintExceptionData = constraintExceptionData;
        }

        public DbConstraintExceptionData DbConstraintExceptionData
        {
            get => (DbConstraintExceptionData)Data[DbConstraintDataKey]!;
            private init => Data[DbConstraintDataKey] = value;
        }

        public override string ToString()
            => DbConstraintExceptionData.ToString();
    }
}
