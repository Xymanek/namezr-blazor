namespace Namezr.Components.Layout;

public interface IStudioPageContext
{
    Task SetCurrentCreatorAndValidateAccess(Guid creatorId);
}