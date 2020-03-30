using ContosoUniversity.Data;
using System;
using System.Threading.Tasks;

namespace ContosoUniversity.DAL.Generic
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        SchoolContext Context { get; }
        Task Commit();
    }

    public class UnitOfWork : IUnitOfWork
    {
        public SchoolContext Context { get; }

        public UnitOfWork(SchoolContext context)
        {
            Context = context;
        }
        public async Task Commit()
        {
            await Context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();

        }
    }
}