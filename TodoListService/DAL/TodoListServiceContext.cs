using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using TodoListService.Models;
using TodoListService.Utils;

namespace TodoListService.DAL
{
    public class TodoListServiceContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, please use data migrations.
        // For more information refer to the documentation:
        // http://msdn.microsoft.com/en-us/data/jj591621.aspx
    
        public TodoListServiceContext() : base("name=TodoListServiceContext")
        {
        }

        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<PerWebUserCache> PerUserCacheList { get; set; }
    
    }
}
