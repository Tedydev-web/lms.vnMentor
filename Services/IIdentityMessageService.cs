using System.Threading.Tasks;

namespace vnMentor.Services
{
    interface IIdentityMessageService
    {
        //
        // Summary:
        //     This method should send the message
        //
        // Parameters:
        //   message:
        Task SendAsync(IdentityMessage message);
    }

    public class IdentityMessage
    {
        //
        // Summary:
        //     Destination, i.e. To email, or SMS phone number
        public string Destination { get; set; }

        //
        // Summary:
        //     Subject
        public string Subject { get; set; }

        //
        // Summary:
        //     Message contents
        public string Body { get; set; }
    }
}
