namespace TicketBooking.Identity.Internal.Persistence;

internal static class IdentityConstraintNames
{
    internal const string SystemUsersPrimaryKey = "PK_SystemUsers";
    internal const string RolesPrimaryKey = "PK_Roles";
    internal const string PermissionsPrimaryKey = "PK_Permissions";
    internal const string SystemUserRolesPrimaryKey = "PK_SystemUserRoles";
    internal const string RolePermissionsPrimaryKey = "PK_RolePermissions";

    internal const string SystemUserRolesSystemUserForeignKey = "FK_SystemUserRoles_SystemUsers_SystemUserId";
    internal const string SystemUserRolesRoleForeignKey = "FK_SystemUserRoles_Roles_RoleId";
    internal const string RolePermissionsRoleForeignKey = "FK_RolePermissions_Roles_RoleId";
    internal const string RolePermissionsPermissionForeignKey = "FK_RolePermissions_Permissions_PermissionId";

    internal const string SystemUsersNormalizedLoginUniqueIndex = "UX_SystemUsers_NormalizedLogin";
    internal const string SystemUsersEmailIndex = "IX_SystemUsers_Email";
    internal const string SystemUsersStatusIndex = "IX_SystemUsers_Status";
    internal const string RolesCodeUniqueIndex = "UX_Roles_Code";
    internal const string PermissionsCodeUniqueIndex = "UX_Permissions_Code";
    internal const string SystemUserRolesPairUniqueIndex = "UX_SystemUserRoles_SystemUserId_RoleId";
    internal const string RolePermissionsPairUniqueIndex = "UX_RolePermissions_RoleId_PermissionId";
}
