using System;

namespace MyFreeSql.Generator.Models;

public class ColumnInfoModel
{
    public string Name { get; set; }
    
    /// <summary>
    /// C#类型
    /// </summary>
    /// <value></value>
    public Type CsType { get; set; }

    // /// <summary>
    // /// 类型名
    // /// </summary>
    // /// <value></value>
    public string CsTypeName { get; set; }

    /// <summary>
    /// 列名
    /// </summary>
    /// <value></value>
    public string CsName { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    /// <value></value>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    /// <value></value>
    public string Comment { get; set; }

    /// <summary>
    /// 长度
    /// </summary>
    /// <value></value>
    public int MaxLength { get; set; }
    
    public string DefaultValue { get; set; }
    
    public string ColAttributeStr { get; set; }
}