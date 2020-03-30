using ContosoUniversity.DAL.Generic;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ContosoUniversity.DAL
{
    public class StudentRepository : IStudentRepository
    {
        private readonly IGenericRepository<Student> _repository;

        public StudentRepository(IGenericRepository<Student> repository)
        {
            _repository = repository;
        }
        public async Task Delete(object id)
        {
            await _repository.Delete(id);
        }

        public void Delete(Student student)
        {
            _repository.Delete(student);
        }

        public async Task<IEnumerable<Student>> Get(Expression<Func<Student, bool>> filter = null, Func<IQueryable<Student>, IOrderedQueryable<Student>> orderBy = null, string includeProperties = "")
        {
            return await _repository.Get(filter, orderBy, includeProperties);
        }

        public async Task<Student> GetByID(object id)
        {
            return await _repository.GetByID(id);
        }

        public async Task Insert(Student student)
        {
            await _repository.Insert(student);
        }

        public void Update(Student student)
        {
            _repository.Update(student);
        }
    }
}