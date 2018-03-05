using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SimpleBlog.Models;

namespace SimpleBlog.Services
{
    public interface IBlogService
    {
        Task<IEnumerable<Article>> GetPosts(int count, int skip = 0);

        Task<IEnumerable<Article>> GetAllPosts();


        Task<Article> GetPostBySlug(string slug);

        Task<Article> GetPostById(string id);
        

        Task SavePost(Article post);

        Task DeletePost(Article post);

        Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);
    }

    public abstract class InMemoryBlogServiceBase : IBlogService
    {
        public InMemoryBlogServiceBase(IHttpContextAccessor contextAccessor)
        {
            ContextAccessor = contextAccessor;
        }

        protected List<Article> Cache { get; set; }
        protected IHttpContextAccessor ContextAccessor { get; }

        public virtual Task<IEnumerable<Article>> GetPosts(int count, int skip = 0)
        {
            var posts = Cache
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);
        }

        public virtual Task<IEnumerable<Article>> GetAllPosts()
        {
            var posts = Cache.Take(Cache.Count);

            return Task.FromResult(posts);
        }

        public virtual Task<Article> GetPostBySlug(string slug)
        {
            var post = Cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
           
            if (post != null)
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Article>(null);
        }

        public virtual Task<Article> GetPostById(string id)
        {
            var post = Cache.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            bool isAdmin = IsAdmin();

            if (post != null)
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Article>(null);
        }

        
        public abstract Task SavePost(Article post);

        public abstract Task DeletePost(Article post);

        public abstract Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null);

        protected void SortCache()
        {
            Cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return ContextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }
    }
}
