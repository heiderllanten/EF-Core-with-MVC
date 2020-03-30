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
        internal IUnitOfWork unitOfWork;
        internal DbSet<T> dbSet;

        public GenericRepository(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
            this.dbSet = unitOfWork.Context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> Get(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<T> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync();
            }
            else
            {
                return await query.ToListAsync();
            }
        }

        public virtual async Task<T> GetByID(object id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual async Task Insert(T entity)
        {
            await dbSet.AddAsync(entity);
        }

        public virtual async Task Delete(object id)
        {
            T entityToDelete = await dbSet.FindAsync(id);
            Delete(entityToDelete);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (unitOfWork.Context.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
        }

        public virtual void Update(T entityToUpdate)
        {
            dbSet.Attach(entityToUpdate);
            unitOfWork.Context.Entry(entityToUpdate).State = EntityState.Modified;
        }
    }
}