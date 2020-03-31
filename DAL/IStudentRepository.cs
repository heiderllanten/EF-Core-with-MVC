using ContosoUniversity.DAL.Generic;
using ContosoUniversity.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ContosoUniversity.DAL
{
    public interface IStudentRepository : IGenericRepository<Student>
    {

    }
}