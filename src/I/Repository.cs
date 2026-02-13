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

using PPWCode.Vernacular.Exceptions.V;
using PPWCode.Vernacular.Persistence.V;
using PPWCode.Vernacular.Persistence.V.Exceptions;

namespace PPWCode.Vernacular.EntityFrameworkCore.I;

/// <inheritdoc />
public abstract class Repository<TModel, TId, TTimestamp> : IRepository<TModel, TId>
    where TModel : class, IPersistentObject<TId>, IIdentity<TId>
    where TId : IEquatable<TId>
    where TTimestamp : struct, IComparable<TTimestamp>, IEquatable<TTimestamp>
{
    private readonly PpwDbContext<TTimestamp> _context;

    protected Repository(PpwDbContext<TTimestamp> dbContext)
    {
        _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc cref="DbContext.Set{TModel}()" />
    private DbSet<TModel> DbSet
        => _context.Set<TModel>();

    /// <inheritdoc cref="DbSet{TModel}.AsQueryable" />
    protected virtual IQueryable<TModel> Queryable
        => DbSet.AsQueryable();

    /// <inheritdoc cref="DbSet{TModel}.FindAsync(object[])" />
    public virtual async Task<TModel?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => await DbSet
               .FindAsync([id], cancellationToken)
               .ConfigureAwait(false);

    /// <inheritdoc cref="DbSet{TModel}.Find" />
    public TModel? GetById(TId id)
        => DbSet.Find(id);

    /// <inheritdoc cref="IRepository{TModel,TId}.FindAllAsync" />
    public virtual async Task<List<TModel>> FindAllAsync(CancellationToken cancellationToken = default)
        => await DbSet
               .ToListAsync(cancellationToken)
               .ConfigureAwait(false);

    /// <inheritdoc cref="IRepository{TModel,TId}.FindAll" />
    public List<TModel> FindAll()
        => DbSet.ToList();

    /// <inheritdoc cref="DbSet{TModel}.Update" />
    public virtual async Task UpdateAsync(TModel model, CancellationToken cancellationToken = default)
    {
        if (!model.IdIsTransient)
        {
            TModel? foundEntity = await GetByIdAsync(model.Id!, cancellationToken).ConfigureAwait(false);
            if (foundEntity is null)
            {
                throw new IdNotFoundException<TModel, TId>(model.Id!);
            }
        }

        DbSet.Update(model);
    }

    /// <inheritdoc cref="DbSet{TModel}.Update" />
    public void Update(TModel model)
    {
        if (!model.IdIsTransient)
        {
            TModel? foundEntity = GetById(model.Id!);
            if (foundEntity is null)
            {
                throw new IdNotFoundException<TModel, TId>(model.Id!);
            }
        }

        DbSet.Update(model);
    }

    /// <inheritdoc cref="DbSet{TModel}.AddRange(TModel[])" />
    public virtual async Task InsertAsync(TModel model, CancellationToken cancellationToken = default)
    {
        if (!model.IdIsTransient)
        {
            throw new ProgrammingError("Adding a non-transient entity is not allowed.");
        }

        await DbSet
            .AddAsync(model, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="DbSet{TModel}.Add" />
    public void Insert(TModel model)
    {
        if (!model.IdIsTransient)
        {
            throw new ProgrammingError("Adding a non-transient entity is not allowed.");
        }

        DbSet.Add(model);
    }

    /// <inheritdoc cref="DbSet{TEntity}.Remove" />
    public virtual Task DeleteAsync(TModel model, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(model);

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="DbSet{TEntity}.Remove" />
    public void Delete(TModel model)
        => DbSet.Remove(model);

    /// <inheritdoc cref="IRepository{TModel,TId}.IsTransient" />
    public virtual bool IsTransient(TModel model)
        => (_context.Entry(model).State == EntityState.Detached) && model.IdIsTransient;

    /// <inheritdoc cref="IRepository{TModel,TId}.FlushAsync"/>
    public virtual async Task FlushAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc cref="IRepository{TModel,TId}.Flush"/>
    public virtual void Flush()
        => FlushAsync()
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     <para>
    ///         Find a specified range of contiguous tuples, defined by <paramref name="take" />, after skipping
    ///         a number of tuples, defined by <paramref name="skip" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="skip">The number of tuples to skip before returning the remaining tuples</param>
    /// <param name="take">Returns a specified range of contiguous tuples from a sequence</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     an awaitable <see cref="Task" />
    /// </returns>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    protected virtual async Task<List<TResult>> FindAsync<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TResult> result = lambda(Queryable);
        if (skip != null)
        {
            result = result.Skip(skip.Value);
        }

        if (take != null)
        {
            result = result.Take(take.Value);
        }

        return await result.ToListAsync(cancellationToken);
    }

    /// <summary>
    ///     <para>
    ///         Find a specified range of contiguous tuples, defined by <paramref name="take" />, after skipping
    ///         a number of tuples, defined by <paramref name="skip" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="skip">The number of tuples to skip before returning the remaining tuples</param>
    /// <param name="take">Returns a specified range of contiguous tuples from a sequence</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <returns>
    ///     A list of tuples of type <typeparamref name="TResult" />
    /// </returns>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    protected List<TResult> Find<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int? skip = null,
        int? take = null)
        => FindAsync(lambda, skip, take)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     <para>
    ///         Find a specified range of contiguous tuples, called a page defined by <paramref name="pageSize" />, after
    ///         skipping
    ///         a number of pages defined by <paramref name="pageIndex" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="pageIndex">The number of pages to skip before returning the remaining tuples</param>
    /// <param name="pageSize">Returns a specified range of contiguous tuples from a sequence, called a page</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     an awaitable <see cref="Task" />
    /// </returns>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    protected virtual async Task<IPagedList<TResult>> FindPagedAsync<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int count =
            await lambda(Queryable)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);
        List<TResult> page =
            await FindAsync(
                    lambda,
                    (pageIndex - 1) * pageSize,
                    pageSize,
                    cancellationToken)
                .ConfigureAwait(false);
        return new PagedList<TResult>(page, pageIndex, pageSize, count);
    }

    /// <summary>
    ///     <para>
    ///         Find a specified range of contiguous tuples, called a page defined by <paramref name="pageSize" />, after
    ///         skipping
    ///         a number of pages defined by <paramref name="pageIndex" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="pageIndex">The number of pages to skip before returning the remaining tuples</param>
    /// <param name="pageSize">Returns a specified range of contiguous tuples from a sequence, called a page</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <returns>
    ///     A page is returned, a page is defined using <see cref="IPagedList{T}" />
    /// </returns>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    protected IPagedList<TResult> FindPaged<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int pageIndex,
        int pageSize)
        => FindPagedAsync(lambda, pageIndex, pageSize)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     <para>
    ///         Find a number of tuples and count them.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     an awaitable <see cref="Task" />
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    protected virtual async Task<int> CountAsync<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        CancellationToken cancellationToken = default)
        => await lambda(Queryable)
               .CountAsync(cancellationToken)
               .ConfigureAwait(false);

    /// <summary>
    ///     <para>
    ///         Find a number of tuples and count them.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <returns>
    ///     The count of tuples
    /// </returns>
    protected int Count<TResult>(Func<IQueryable<TModel>, IQueryable<TResult>> lambda)
        => CountAsync(lambda)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

    /// <summary>
    ///     <para>
    ///         Find a tuple at <paramref name="index" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="index">The index of the tuple to be returned</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <returns>
    ///     an awaitable <see cref="Task" />
    /// </returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    protected virtual async Task<TResult?> GetAtIndexAsync<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int index,
        CancellationToken cancellationToken = default)
        => await
               lambda(Queryable)
                   .Skip(index)
                   .Take(1)
                   .SingleOrDefaultAsync(cancellationToken)
                   .ConfigureAwait(false);

    /// <summary>
    ///     <para>
    ///         Find a tuple at <paramref name="index" />.
    ///     </para>
    ///     <para>The <paramref name="lambda" /> describes the predicate to be used.</para>
    /// </summary>
    /// <param name="lambda">The predicate to be used to query our store</param>
    /// <param name="index">The index of the tuple to be returned</param>
    /// <typeparam name="TResult">
    ///     The <see cref="Type" /> of the result. It doesn't need to be of type
    ///     <typeparamref name="TModel" />.
    /// </typeparam>
    /// <returns>
    ///     The tuple at <paramref name="index" /> of the tuples that satisfied the <paramref name="lambda" />.
    /// </returns>
    /// <remarks>
    ///     Remember to specify an order, otherwise the result isn't deterministic.
    /// </remarks>
    protected TResult? GetAtIndex<TResult>(
        Func<IQueryable<TModel>, IQueryable<TResult>> lambda,
        int index)
        => GetAtIndexAsync(lambda, index)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
}
