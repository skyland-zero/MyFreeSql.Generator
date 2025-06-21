using System;
using System.Collections.Generic;
using System.Linq;
using FreeSql.DatabaseModel;

namespace MyFreeSql.Generator.Models;

public class TableInfoModel
{
    public TableInfoModel(string argsNameSpace, DbTableInfo table, IFreeSql fsqlIn)
    {
        Namespace = argsNameSpace;
        TableInfo = table;
        fsql = fsqlIn;
    }

    private List<string> AwsProperties =
        ["ID", "CREATEDATE", "CREATEUSER", "ORGID", "PROCESSDEFID", "UPDATEDATE", "UPDATEUSER", "BINDID", "ISEND"];

    /// <summary>
    /// FreeSql获取的表信息
    /// </summary>
    private DbTableInfo TableInfo;

    private IFreeSql fsql;
    private string Suffix;

    /// <summary>
    /// 表名称
    /// </summary>
    public string TableName => TableInfo.Name;

    public string TableCsName => TableName;

    /// <summary>
    /// 是否是Aws表
    /// </summary>
    public bool IsAwsEntity
    {
        get
        {
            var names = TableInfo.Columns.Select(a => a.Name).ToList();
            if (AwsProperties.All(a => names.Any(x => x == a)))
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 表注释
    /// </summary>
    public string TableComment => string.IsNullOrWhiteSpace(TableInfo.Comment) ? TableInfo.Name : TableInfo.Comment;

    /// <summary>
    /// 命名空间
    /// </summary>
    public string Namespace { get; set; }

    /// <summary>
    /// 基类
    /// </summary>
    public string Extends
    {
        get
        {
            if (IsAwsEntity)
            {
                return " : AWSEntity";
            }

            return "";
        }
    }


    /// <summary>
    /// 列
    /// </summary>
    /// <value></value>
    public List<ColumnInfoModel> Columns
    {
        get
        {
            if (TableInfo.Columns == null)
            {
                return new List<ColumnInfoModel>();
            }
            else
            {
                var list = TableInfo.Columns.OrderBy(x => x.Position).Select(x => new ColumnInfoModel
                {
                    Name = x.Name,
                    CsType = x.CsType,
                    CsTypeName = fsql.DbFirst.GetCsType(x),
                    CsName = Utils.GetCsName(x.Name),
                    IsNullable = x.IsNullable,
                    Comment = string.IsNullOrWhiteSpace(x.Coment) ? x.Name : x.Comment,
                    MaxLength = x.MaxLength,
                    DefaultValue = Utils.GetColumnDefaultValue(fsql, x, false),
                    ColAttributeStr = Utils.GetColumnAttribute(fsql, x)
                }).ToList();

                if (IsAwsEntity)
                {
                    list = list.Where(x => !AwsProperties.Contains(x.Name)).ToList();
                }

                return list;
            }
        }
    }

    // public List<ColumnInfoModel> Columns { get; set; }
}