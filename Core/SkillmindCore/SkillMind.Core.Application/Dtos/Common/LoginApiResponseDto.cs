namespace SkillMind.Core.Application.Dtos.Common
{
    public class LoginApiResponseDto
    {
        public LoginUserData? Data { get; set; }
        public bool HasError { get; set; }
        public List<string> Errors { get; set; } = [];
    }

    public class LoginUserData
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string LastName { get; set; }
        public string? Email { get; set; }
        public required List<string> Roles { get; set; }
        public bool IsVerified { get; set; }
        public required string JwtToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}