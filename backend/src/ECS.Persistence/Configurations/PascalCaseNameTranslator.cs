using Npgsql.NameTranslation;

namespace ECS.Persistence.Configurations;

/// <summary>
/// Identity name translator: keeps CLR enum type/member names exactly as-is so the
/// PostgreSQL enum labels match the schema (which uses PascalCase, e.g. 'InMaintenance',
/// 'Completed'). Without this, Npgsql's default snake_case translator would look for
/// labels like 'in_maintenance' and fail to bind.
/// </summary>
public sealed class PascalCaseNameTranslator : INpgsqlNameTranslator
{
    public string TranslateTypeName(string clrName) => clrName;
    public string TranslateMemberName(string clrName) => clrName;
}
