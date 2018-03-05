using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleBlog.Models;
using SimpleBlog.ViewModels;
using SimpleBlog.Services;

namespace SimpleBlog.Controllers
{
    public class BlogController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var articles = await _blog.GetAllPosts();
            articles.ToList();
            
            return View("Index");
        }

        [Route("/get")]
        [HttpGet]
        public async Task<IActionResult> GetPosts()
        {
            var articles = await _blog.GetAllPosts();
            articles.ToList();
            
            return Ok(articles);
        }
        
        private readonly IBlogService _blog;

        
        public BlogController(IBlogService blog)
        {
            _blog = blog;
        }
        
        [Route("/blog/edit/")]
        [Authorize]
        public ActionResult New()
        {
            return View("Edit", new Article());
        }

        [Route("/blog/{slug?}")]
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Article article)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", new Article());
            }
            
            var existing = await _blog.GetPostById(article.Id) ?? article;

            existing.Title = article.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(article.Slug) ? article.Slug.Trim() : Models.Article.CreateSlug(article.Title);
            existing.Content = article.Content.Trim();
            existing.Excerpt = article.Excerpt.Trim();

            await _blog.SavePost(existing);

            return Redirect(article.GetLink());
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet, Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return View(new Article());
            }

            var article = await _blog.GetPostById(id);

            if (article != null)
            {
                return View(article);
            }

            return NotFound();
            
        }

        [Route("/blog/{slug?}")]
        public async Task<IActionResult> Details(string slug)
        {
            var article = await _blog.GetPostBySlug(slug);

            if (article == null)
                return NotFound();
            

            return View("Post", article);
        }

        [Route("/blog/deletepost/{id}")]
        [HttpPost, Authorize, AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            var existing = await _blog.GetPostById(id);

            if (existing != null)
            {
                await _blog.DeletePost(existing);
                return Redirect("/");
            }

            return NotFound();
        }

        [Route("/blog/comment/{articleId}")]
        [HttpPost]
        public async Task<IActionResult> AddComment(string articleId, Comment comment)
        {
            var article = await _blog.GetPostById(articleId);

            if (!ModelState.IsValid)
            {
                return View("Post", new Article());
            }

            if (article == null)
            {
                return NotFound();
            }

            comment.IsAdmin = User.Identity.IsAuthenticated;
            comment.Content = comment.Content.Trim();

            if (User.Identity.IsAuthenticated)
            {
                comment.Author = "Alidar Asvarov".Trim();
                comment.Email = "alidar.asvarov@simpleblog.com".Trim();
            }
            else
            {
                comment.Author = comment.Author.Trim();
                comment.Email = comment.Email.Trim();
            }
            
            article.Comments.Add(comment);
            await _blog.SavePost(article);

            return Redirect(article.GetLink() + "#" + comment.Id);
        }

        [Route("/blog/comment/{articleId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string articleId, string commentId)
        {
            var article = await _blog.GetPostById(articleId);

            if (article == null)
            {
                return NotFound();
            }

            var comment = article.Comments.FirstOrDefault(c => c.Id.Equals(commentId, StringComparison.OrdinalIgnoreCase));

            if (comment == null)
            {
                return NotFound();
            }

            
            article.Comments.Remove(comment);
            await _blog.SavePost(article);

            return Redirect(article.GetLink() + "#comments");
        }

    }
}