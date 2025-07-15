using System.ComponentModel.DataAnnotations;

namespace PQBI.Authorization.Users.Dto
{
    public class ChangeUserLanguageDto
    {
        [Required]
        public string LanguageName { get; set; }
    }
}
