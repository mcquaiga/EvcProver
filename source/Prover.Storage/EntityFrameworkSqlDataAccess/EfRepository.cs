using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prover.Shared.Domain;
using Prover.Shared.Interfaces;
using Prover.Shared.Storage.Interfaces;
using Prover.Storage.EntityFrameworkSqlDataAccess.Storage;

namespace Prover.Storage.EntityFrameworkSqlDataAccess
{
    /// <summary>
    ///     "There's some repetition here - couldn't we have some the sync methods call the async?"
    ///     https://blogs.msdn.microsoft.com/pfxteam/2012/04/13/should-i-expose-synchronous-wrappers-for-asynchronous-methods/
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EfRepository<T> : IAsyncRepository<T>
        where T : AggregateRoot
    {
        #region Fields

        protected readonly ProverDbContext Context;

        #endregion

        #region Constructors

        public EfRepository(ProverDbContext dbContext)
        {
            Context = dbContext;
        }

        #endregion

        #region Private

        private IQueryable<T> ApplySpecification(ISpecification<T> spec)
        {
            return SpecificationEvaluator<T>.GetQuery(Context.Set<T>().AsQueryable(), spec);
        }

        #endregion

        #region Methods

        public virtual async Task<T> UpsertAsync(T entity)
        {
            Context.Set<T>().Add(entity);
            await Context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task<int> CountAsync(ISpecification<T> spec)
        {
            return await ApplySpecification(spec).CountAsync();
        }
        
        public virtual  async Task DeleteAsync(T entity)
        {
            Context.Set<T>().Remove(entity);
            await Context.SaveChangesAsync();
        }

        public virtual Task DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<T> GetAsync(Guid id)
        {
            return await Context.Set<T>().FindAsync(new[] {id});
        }

        public virtual Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<T>> ListAsync()
        {
            return await Context.Set<T>().ToListAsync();
        }

        public IEnumerable<T> Query(Expression<Func<T, bool>> predicate = null) => throw new NotImplementedException();

        public virtual async Task<T> GetByIdAsync(Guid id)
        {
            return await Context.Set<T>().FindAsync(id);
        }

        public virtual async Task<IReadOnlyList<T>> ListAllAsync()
        {
            return await Context.Set<T>().ToListAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            Context.Entry(entity).State = EntityState.Modified;
            await Context.SaveChangesAsync();
        }

        #endregion
    }
}