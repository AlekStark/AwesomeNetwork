using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AwesomeNetwork.Data.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        //protected DbContext _db;
        private readonly ApplicationDbContext _context;
        public DbSet<T> Set
        {
            get;
            private set;
        }

        public Repository(ApplicationDbContext context)
        {
            //_db = db;
            _context = context;
            var set = _context.Set<T>();
            set.Load();

            Set = set;
        }

        public void Create(T item)
        {
            Set.Add(item);
            _context.SaveChanges();
        }

        public void Delete(T item)
        {
            Set.Remove(item);
            _context.SaveChanges();
        }

        public T Get(int id)
        {
            return Set.Find(id);
        }

        public IEnumerable<T> GetAll()
        {
            return Set;
        }

        public void Update(T item)
        {
            Set.Update(item);
            _context.SaveChanges();
        }
    }
}