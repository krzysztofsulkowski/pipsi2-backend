using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_api.Migrations
{
    /// <inheritdoc />
    public partial class AddGetUsersWithRolesFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.get_users_with_roles;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION public.get_users_with_roles(search_value text DEFAULT NULL::text, sort_column text DEFAULT 'UserName'::text, sort_direction text DEFAULT 'ASC'::text)
 RETURNS TABLE(userid text, username character varying, email character varying, roleid text, rolename character varying, islocked boolean)
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN QUERY
    SELECT 
        u.""Id"" as userId,
        u.""UserName"" as userName,
        u.""Email"" as email,
        r.""Id"" as roleId,
        r.""Name"" as roleName,
        (u.""LockoutEnd"" IS NOT NULL AND u.""LockoutEnd"" > now() AT TIME ZONE 'UTC') as islocked
    FROM ""AspNetUsers"" u
    LEFT JOIN ""AspNetUserRoles"" ur ON u.""Id"" = ur.""UserId""
    LEFT JOIN ""AspNetRoles"" r ON ur.""RoleId"" = r.""Id""
    WHERE 
        (search_value IS NULL OR search_value = ''
        OR u.""UserName"" ILIKE '%' || search_value || '%'
        OR u.""Email"" ILIKE '%' || search_value || '%'
        OR r.""Name"" ILIKE '%' || search_value || '%')
    ORDER BY
        CASE 
            WHEN sort_column = 'UserName' AND sort_direction = 'ASC' THEN u.""UserName""
        END ASC,
        CASE 
            WHEN sort_column = 'UserName' AND sort_direction = 'DESC' THEN u.""UserName""
        END DESC,
        CASE 
            WHEN sort_column = 'Email' AND sort_direction = 'ASC' THEN u.""Email""
        END ASC,
        CASE 
            WHEN sort_column = 'Email' AND sort_direction = 'DESC' THEN u.""Email""
        END DESC,
        CASE 
            WHEN sort_column = 'Role' AND sort_direction = 'ASC' THEN r.""Name""
        END ASC,
        CASE 
            WHEN sort_column = 'Role' AND sort_direction = 'DESC' THEN r.""Name""
        END DESC,
        CASE 
            WHEN sort_column IS NULL OR sort_column = '' OR sort_column NOT IN ('UserName', 'Email', 'Role') THEN u.""UserName""
        END ASC;
END;
$function$;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.get_users_with_roles;");
        }


    }
}
