using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dima.Core.Requests.Categories
{
    public class UpdateCategoryRequest:Request
    {
        public long Id { get; set; }
        [Required(ErrorMessage = "Invalid Category Name")]
        [MaxLength(80, ErrorMessage = "Maximum Lenght=80 characters")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Invalid Description")]
        public string? Description { get; set; }

    }
}
