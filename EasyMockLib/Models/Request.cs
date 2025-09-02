namespace EasyMockLib.Models
{
    public class Request
    {
        public Request()
        {
            RequestBody = new Body();
        }
        public Body RequestBody { get; set; }
    }
}
