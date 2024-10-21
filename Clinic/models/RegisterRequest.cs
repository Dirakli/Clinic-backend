namespace Clinic.models
{
    public class RegisterRequest
    {
        public string name { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public string private_number { get; set; }
        public string category { get; set; }
        public IFormFile? photo { get; set; }
        public IFormFile? resume { get; set; }
    }
}
