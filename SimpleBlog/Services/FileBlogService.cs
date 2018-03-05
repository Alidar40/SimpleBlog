using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SimpleBlog.Models;

namespace SimpleBlog.Services
{
    public class FileBlogService : IBlogService
    {
        private readonly List<Article> _cache = new List<Article>();
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly string _folder;

        public FileBlogService(IHostingEnvironment env, IHttpContextAccessor contextAccessor)
        {
            _folder = Path.Combine(env.WebRootPath, "Posts");
            _contextAccessor = contextAccessor;

            Initialize();
        }

        public virtual Task<IEnumerable<Article>> GetPosts(int count, int skip = 0)
        {
            var posts = _cache
                .Skip(skip)
                .Take(count);

            return Task.FromResult(posts);
        }

        public virtual Task<IEnumerable<Article>> GetAllPosts()
        {
            var posts = _cache.Take(_cache.Count);

            return Task.FromResult(posts);
        }


        public virtual Task<Article> GetPostBySlug(string slug)
        {
            var article = _cache.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            if (article != null)
            {
                return Task.FromResult(article);
            }

            return Task.FromResult<Article>(null);
        }

        public virtual Task<Article> GetPostById(string id)
        {
            var post = _cache.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (post != null)
            {
                return Task.FromResult(post);
            }

            return Task.FromResult<Article>(null);
        }

        
        public async Task SavePost(Article article)
        {
            string filePath = GetFilePath(article);
            article.LastModified = DateTime.UtcNow.ToLocalTime();

            XDocument doc = new XDocument(
                            new XElement("post",
                                new XElement("title", article.Title),
                                new XElement("slug", article.Slug),
                                new XElement("pubDate", article.PubDate.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("lastModified", article.LastModified.ToString("yyyy-MM-dd HH:mm:ss")),
                                new XElement("excerpt", article.Excerpt),
                                new XElement("content", article.Content),
                                new XElement("comments", string.Empty)
                            ));

            
            XElement comments = doc.XPathSelectElement("post/comments");
            foreach (Comment comment in article.Comments)
            {
                comments.Add(
                    new XElement("comment",
                        new XElement("author", comment.Author),
                        new XElement("email", comment.Email),
                        new XElement("date", comment.PubDate.ToString("yyyy-MM-dd HH:m:ss")),
                        new XElement("content", comment.Content),
                        new XAttribute("isAdmin", comment.IsAdmin),
                        new XAttribute("id", comment.Id)
                    ));
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            {
                await doc.SaveAsync(fs, SaveOptions.None, CancellationToken.None).ConfigureAwait(false);
            }

            if (!_cache.Contains(article))
            {
                _cache.Add(article);
                SortCache();
            }
        }

        public Task DeletePost(Article article)
        {
            string filePath = GetFilePath(article);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (_cache.Contains(article))
            {
                _cache.Remove(article);
            }

            return Task.CompletedTask;
        }

        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = suffix ?? DateTime.UtcNow.ToLocalTime().Ticks.ToString();

            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);

            string relative = $"files/{name}_{suffix}{ext}";
            string absolute = Path.Combine(_folder, relative);
            string dir = Path.GetDirectoryName(absolute);

            Directory.CreateDirectory(dir);
            using (var writer = new FileStream(absolute, FileMode.CreateNew))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }

            return "/Posts/" + relative;
        }

        private string GetFilePath(Article article)
        {
            return Path.Combine(_folder, article.Id + ".xml");
        }

        private void Initialize()
        {
            LoadPosts();
            SortCache();
        }

        private void LoadPosts()
        {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);

            // Can this be done in parallel to speed it up?
            foreach (string file in Directory.EnumerateFiles(_folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                XElement doc = XElement.Load(file);

                Article post = new Article
                {
                    Id = Path.GetFileNameWithoutExtension(file),
                    Title = ReadValue(doc, "title"),
                    Excerpt = ReadValue(doc, "excerpt"),
                    Content = ReadValue(doc, "content"),
                    Slug = ReadValue(doc, "slug").ToLowerInvariant(),
                    PubDate = DateTime.Parse(ReadValue(doc, "pubDate")),
                    LastModified = DateTime.Parse(ReadValue(doc, "lastModified", DateTime.Now.ToLocalTime().ToString(CultureInfo.InvariantCulture))),
                };
                
                LoadComments(post, doc);
                _cache.Add(post);
            }
        }
        

        private static void LoadComments(Article article, XElement doc)
        {
            var comments = doc.Element("comments");

            if (comments == null)
                return;

            foreach (var node in comments.Elements("comment"))
            {
                Comment comment = new Comment()
                {
                    Id = ReadAttribute(node, "id"),
                    Author = ReadValue(node, "author"),
                    Email = ReadValue(node, "email"),
                    IsAdmin = bool.Parse(ReadAttribute(node, "isAdmin", "false")),
                    Content = ReadValue(node, "content"),
                    PubDate = DateTime.Parse(ReadValue(node, "date", "2000-01-01")),
                };

                article.Comments.Add(comment);
            }
        }

        private static string ReadValue(XElement doc, XName name, string defaultValue = "")
        {
            if (doc.Element(name) != null)
                return doc.Element(name)?.Value;

            return defaultValue;
        }

        private static string ReadAttribute(XElement element, XName name, string defaultValue = "")
        {
            if (element.Attribute(name) != null)
                return element.Attribute(name)?.Value;

            return defaultValue;
        }
        protected void SortCache()
        {
            _cache.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
        }

        protected bool IsAdmin()
        {
            return _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
        }

    }
}
