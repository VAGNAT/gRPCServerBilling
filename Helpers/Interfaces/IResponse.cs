namespace Billing.Helpers.Interfaces
{
    public interface IResponse
    {
        Response ResponseFail(string comment);
        Response ResponseOk(string comment);
    }
}
