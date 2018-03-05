using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SimpleBlog.Models
{
    public class Article
    {
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Title { get; set; }
        
        public string Content { get; set; }

        [Required]
        public string Slug { get; set; }

        [Required]
        public string Excerpt { get; set; }

        public DateTime PubDate { get; set; } = DateTime.UtcNow.ToLocalTime();

        public DateTime LastModified { get; set; } = DateTime.UtcNow.ToLocalTime();

        public IList<Comment> Comments { get; } = new List<Comment>();

        public string GetLink()
        {
            return $"/blog/{Slug}/";
        }

        public bool AreCommentsOpen(int commentsCloseAfterDays)
        {
            return PubDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow.ToLocalTime();
        }

        public static string CreateSlug(string title)
        {
            title = title.ToLowerInvariant().Replace(" ", "-");
            title = RemoveReservedUrlCharacters(title);

            return title.ToLowerInvariant();
        }

        static string RemoveReservedUrlCharacters(string text)
        {
            var reservedCharacters = new List<string> { "!", "#", "$", "&", "'", "(", ")", "*", ",", "/", ":", ";", "=", "?", "@", "[", "]", "\"", "%", ".", "<", ">", "\\", "^", "_", "'", "{", "}", "|", "~", "`", "+" };

            foreach (var chr in reservedCharacters)
            {
                text = text.Replace(chr, "");
            }

            return text;
        }

        public string RenderContent()
        {
            var result = Content;
            
            return result;
        }
    }
}
