namespace SyaBot.Shared
{
    public class RegisterRequest : RequestModel<RegisterRequest>
    {
        public RegisterRequest(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
