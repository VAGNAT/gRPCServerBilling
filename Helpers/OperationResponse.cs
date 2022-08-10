using Billing.Helpers.Interfaces;

namespace Billing.Helpers
{
    public class OperationResponse : IResponse
    {
        public Response ResponseFail(string comment)
        {
            return new Response() { Status = Response.Types.Status.Failed, Comment = comment };
        }

        public Response ResponseOk(string comment)
        {
            return new Response() { Status = Response.Types.Status.Ok, Comment = comment };
        }
    }
}
