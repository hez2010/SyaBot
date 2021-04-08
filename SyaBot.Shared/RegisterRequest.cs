namespace SyaBot.Shared
{
    public class RegisterRequest : SyaRequest
    {
        public RegisterRequest(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
