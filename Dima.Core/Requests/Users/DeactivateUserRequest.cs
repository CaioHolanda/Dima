namespace Dima.Core.Requests.Users;

public class DeactivateUserRequest
{
    // Assigned by the API from the authenticated principal.
    public long ActorId { get; set; }

    public long Id { get; set; }
}