using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFreeSql.Generator.Models
{
    /// <summary>
    /// Scriban模板引擎使用的模型
    /// </summary>
    public class ScribanModel
    {
        /// <summary>
        /// init
        /// </summary>
        /// <param name="razorModel"></param>
        public ScribanModel(RazorModel razorModel)
        {
            this.RazorModel = razorModel;
        }

        /// <summary>
        /// Razor模板数据
        /// </summary>
        /// <value></value>
        public RazorModel RazorModel { get; set; }

        /// <summary>
        /// 命名空间
        /// </summary>
        /// <value></value>
        public string NameSpace => RazorModel.NameSpace;

        /// <summary>
        /// 模块名称
        /// </summary>
        /// <value></value>
        public string ModuleName => RazorModel.ModuleName;

        /// <summary>
        /// 表名
        /// </summary>
        /// <value></value>
        public string FullTableName => RazorModel.FullTableName;

        /// <summary>
        /// 表类名
        /// </summary>
        /// <value></value>
        public string TableCsName => RazorModel.GetCsName(FullTableName);

        /// <summary>
        /// 表注释
        /// </summary>
        /// <value></value>
        public string TableComment => RazorModel.TableComment.Replace("\r\n", "\n").Replace("\n", "\r\n       /// ");

        /// <summary>
        /// 列
        /// </summary>
        /// <value></value>
        public List<Column> Columns
        {
            get
            {
                if (RazorModel.columns == null)
                {
                    return new List<Column>();
                }
                else
                {
                    return RazorModel.columns.Select(x => new Column
                    {
                        CsType = x.CsType,
                        CsName = RazorModel.GetCsName(x.Name),
                        IsNullable = x.IsNullable,
                        Comment = x.Coment,
                        MaxLength = x.MaxLength
                    }).ToList();
                }

            }
        }

    }

    /// <summary>
    /// 列
    /// </summary>
    public class Column
    {
        /// <summary>
        /// C#类型
        /// </summary>
        /// <value></value>
        public Type CsType { get; set; }

        /// <summary>
        /// 类型名
        /// </summary>
        /// <value></value>
        public string CsTypeName => CsType.Name;

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
    }
}