using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ContosoUniversity.DAL.Generic
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        internal IUnitOfWork _unitOfWork;
        internal DbSet<T> _dbSet;

        public GenericRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _dbSet = unitOfWork.Context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> Get(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            bool asNoTracking = false)
        {
            return await CreateQuerableForGet(filter, orderBy, includeProperties, asNoTracking).ToListAsync();
        }

        public virtual IQueryable<T> GetAsQuerable(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            bool asNoTracking = false)
        {
            return CreateQuerableForGet(filter, orderBy, includeProperties, asNoTracking);
        }

        public virtual async Task<T> GetByID(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task Insert(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (_unitOfWork.Context.Entry(entityToDelete).State == EntityState.Detached)
            {
                _dbSet.Attach(entityToDelete);
            }
            _dbSet.Remove(entityToDelete);
        }

        public virtual void Update(T entityToUpdate)
        {
            _dbSet.Attach(entityToUpdate);
            _unitOfWork.Context.Entry(entityToUpdate).State = EntityState.Modified;
        }

        private IQueryable<T> CreateQuerableForGet(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            bool asNoTracking = false)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (orderBy != null)
            {
                return orderBy(query);
            }
            else
            {
                return query;
            }
        }
    }
}