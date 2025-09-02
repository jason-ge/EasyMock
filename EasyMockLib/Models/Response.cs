using System.ComponentModel;
using System.Net;

namespace EasyMockLib.Models
{
    public class Response
    {
        public Response()
        {
            ResponseBody = new Body();
        }
        public Body ResponseBody { get; set; }
        private HttpStatusCode _statusCode;
        public HttpStatusCode StatusCode
        {
            get => _statusCode;
            set
            {
                if (_statusCode != value)
                {
                    _statusCode = value;
                    OnPropertyChanged(nameof(StatusCode));
                }
            }
        }
        public int Delay { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
