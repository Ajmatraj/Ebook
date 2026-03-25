namespace EBookNepal.DTOS
{
    public class UpdateProfileDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}