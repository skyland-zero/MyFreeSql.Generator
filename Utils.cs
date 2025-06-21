using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FreeSql;
using FreeSql.DatabaseModel;
using FreeSql.Internal.CommonProvider;
using MySqlConnector;

namespace MyFreeSql.Generator;

public class Utils
{
    public static string GetCsName(string name)
    {
        name = Regex.Replace(name.TrimStart('@', '.'), @"[^\w]", "_");
        name = char.IsLetter(name, 0) ? name : string.Concat("_", name);

        return ConvertToPascalCase(name);
    }


    public static string ConvertToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        string[] parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = textInfo.ToTitleCase(parts[i].ToLower());
            }
        }

        return string.Join("", parts);
    }

    public static string GetColumnAttribute(IFreeSql fsql, DbColumnInfo col, bool isInsertValueSql = false)
    {
        var sb = new List<string>();

        if (GetCsName(col.Name) != col.Name)
            sb.Add("Name = \"" + col.Name + "\"");

        if (col.CsType != null)
        {
            var dbinfo = fsql.CodeFirst.GetDbInfo(col.CsType);
            if (dbinfo != null &&
                string.Compare(dbinfo.dbtypeFull.Replace("NOT NULL", "").Trim(), col.DbTypeTextFull, true) != 0)
            {
                #region StringLength 反向

                switch (fsql.Ado.DataType)
                {
                    case DataType.MySql:
                    case DataType.OdbcMySql:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "longtext": sb.Add("StringLength = -2"); break;
                            case "text": sb.Add("StringLength = -1"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^varchar\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.SqlServer:
                    case DataType.OdbcSqlServer:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "nvarchar(max)": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^nvarchar\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.PostgreSQL:
                    case DataType.OdbcPostgreSQL:
                    case DataType.KingbaseES:
                    case DataType.ShenTong:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "text": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^varchar\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.Oracle:
                    case DataType.OdbcOracle:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "nclob": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^nvarchar2\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.Dameng:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "text": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^nvarchar2\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.Sqlite:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "text": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^nvarchar\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                    case DataType.MsAccess:
                        switch (col.DbTypeTextFull.ToLower())
                        {
                            case "longtext": sb.Add("StringLength = -2"); break;
                            default:
                                var m_stringLength = Regex.Match(col.DbTypeTextFull, @"^varchar\s*\((\w+)\)$",
                                    RegexOptions.IgnoreCase);
                                if (m_stringLength.Success) sb.Add($"StringLength = {m_stringLength.Groups[1].Value}");
                                else sb.Add("DbType = \"" + col.DbTypeTextFull + "\"");
                                break;
                        }

                        break;
                }

                #endregion
            }

            if (col.IsPrimary)
                sb.Add("IsPrimary = true");
            if (col.IsIdentity)
                sb.Add("IsIdentity = true");

            if (dbinfo != null && dbinfo.isnullable != col.IsNullable)
            {
                if (col.IsNullable && fsql.DbFirst.GetCsType(col).Contains("?") == false && col.CsType.IsValueType)
                    sb.Add("IsNullable = true");
                if (col.IsNullable == false && fsql.DbFirst.GetCsType(col).Contains("?") == true)
                    sb.Add("IsNullable = false");
            }

            if (isInsertValueSql)
            {
                var defval = GetColumnDefaultValue(fsql, col, false);
                if (defval == null) //c#默认属性值，就不需要设置 InsertValueSql 了
                {
                    defval = GetColumnDefaultValue(fsql, col, true);
                    if (defval != null)
                    {
                        sb.Add("InsertValueSql = \"" + defval.Replace("\"", "\\\"") + "\"");
                        sb.Add("CanInsert = false");
                    }
                }
                else
                    sb.Add("CanInsert = false");
            }
        }

        if (sb.Any() == false) return null;
        return "[Column(" + string.Join(", ", sb) + ")]";
    }

    public static string GetColumnDefaultValue(IFreeSql fsql, DbColumnInfo col, bool isInsertValueSql)
    {
        var defval = col.DefaultValue?.Trim();
        if (string.IsNullOrEmpty(defval)) return null;
        var cstype = col.CsType.NullableTypeOrThis();
        if (fsql.Ado.DataType == DataType.SqlServer || fsql.Ado.DataType == DataType.OdbcSqlServer)
        {
            if (defval.StartsWith("((") && defval.EndsWith("))")) defval = defval.Substring(2, defval.Length - 4);
            else if (defval.StartsWith("('") && defval.EndsWith("')"))
                defval = defval.Substring(2, defval.Length - 4).Replace("''", "'");
            else if (defval.StartsWith("(") && defval.EndsWith(")")) defval = defval.Substring(1, defval.Length - 2);
            else return null;
        }
        else if ((cstype == typeof(string) && defval.StartsWith("'") && defval.EndsWith("'::character varying") ||
                  cstype == typeof(Guid) && defval.StartsWith("'") && defval.EndsWith("'::uuid")
                 ) && (fsql.Ado.DataType == DataType.PostgreSQL || fsql.Ado.DataType == DataType.OdbcPostgreSQL ||
                       fsql.Ado.DataType == DataType.KingbaseES ||
                       fsql.Ado.DataType == DataType.ShenTong))
        {
            defval = defval.Substring(1, defval.LastIndexOf("'::") - 1).Replace("''", "'");
        }
        else if (defval.StartsWith("'") && defval.EndsWith("'"))
        {
            defval = defval.Substring(1, defval.Length - 2).Replace("''", "'");
            if (fsql.Ado.DataType == DataType.MySql || fsql.Ado.DataType == DataType.OdbcMySql)
                defval = defval.Replace("\\\\", "\\");
        }

        if (cstype.IsNumberType() && decimal.TryParse(defval, out var trydec))
        {
            if (isInsertValueSql) return defval;
            if (cstype == typeof(float)) return defval + "f";
            if (cstype == typeof(double)) return defval + "d";
            if (cstype == typeof(decimal)) return defval + "M";
            return defval;
        }

        if (cstype == typeof(Guid) && Guid.TryParse(defval, out var tryguid))
            return isInsertValueSql
                ? (fsql.Select<TestTb>() as Select0Provider)._commonUtils.FormatSql("{0}", defval)
                : $"Guid.Parse(\"{defval.Replace("\r\n", "\\r\\n").Replace("\"", "\\\"")}\")";
        if (cstype == typeof(DateTime) && DateTime.TryParse(defval, out var trydt))
            return isInsertValueSql
                ? (fsql.Select<TestTb>() as Select0Provider)._commonUtils.FormatSql("{0}", defval)
                : $"DateTime.Parse(\"{defval.Replace("\r\n", "\\r\\n").Replace("\"", "\\\"")}\")";
        if (cstype == typeof(TimeSpan) && TimeSpan.TryParse(defval, out var tryts))
            return isInsertValueSql
                ? (fsql.Select<TestTb>() as Select0Provider)._commonUtils.FormatSql("{0}", defval)
                : $"TimeSpan.Parse(\"{defval.Replace("\r\n", "\\r\\n").Replace("\"", "\\\"")}\")";
        if (cstype == typeof(string))
            return isInsertValueSql
                ? (fsql.Select<TestTb>() as Select0Provider)._commonUtils.FormatSql("{0}", defval)
                : $"\"{defval.Replace("\r\n", "\\r\\n").Replace("\"", "\\\"")}\"";
        if (cstype == typeof(bool))
            return isInsertValueSql ? defval : (defval == "1" || defval == "t" ? "true" : "false");
        if (fsql.Ado.DataType == DataType.MySql || fsql.Ado.DataType == DataType.OdbcMySql)
            if (col.DbType == (int)MySqlDbType.Enum || col.DbType == (int)MySqlDbType.Set)
                if (isInsertValueSql)
                    return (fsql.Select<TestTb>() as Select0Provider)._commonUtils.FormatSql("{0}", defval);
        return isInsertValueSql ? defval : null; //sql function or exp
    }
}