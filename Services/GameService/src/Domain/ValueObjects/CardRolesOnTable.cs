namespace GameService.Domain;

public static class CardRolesOnTable
{
    public record Attack : CardRoleOnTable;

    public record Defence(string BeatenCardId) : CardRoleOnTable;
}
