﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ContosoUniversity.DAL.Generic
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> Get(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            bool asNoTracking = false);

        IQueryable<T> GetAsQuerable(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string includeProperties = "",
            bool asNoTracking = false);

        Task<T> GetByID(object id);

        Task Insert(T entity);
        
        void Delete(T entityToDelete);

        void Update(T entityToUpdate);
    }
}
