using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FreeSql;
using MyFreeSql.Generator.Models;
using RazorEngine.Templating;
using Scriban;
using Console = Colorful.Console;

namespace MyFreeSql.Generator
{
    class ConsoleApp
    {
        string ArgsRazor { get; }
        bool[] ArgsNameOptions { get; }
        string ArgsNameSpace { get; }
        DataType ArgsDbType { get; }
        string ArgsConnectionString { get; }
        string ArgsFilter { get; }
        string ArgsMatch { get; }
        string ArgsFileName { get; }
        internal string ArgsOutput { get; private set; }

        public ConsoleApp(string[] args, ManualResetEvent wait)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var gb2312 = Encoding.GetEncoding("GB2312");
            if (gb2312 != null)
            {
                try
                {
                    Console.OutputEncoding = gb2312;
                    Console.InputEncoding = gb2312;
                }
                catch { }
            }
            var version = "v" + string.Join(".", typeof(ConsoleApp).Assembly.GetName().Version.ToString().Split('.').Where((a, b) => b <= 2));
            Console.WriteAscii(" LDFCore", Color.Violet);
            Console.WriteFormatted(@"
  # Github # {0} {1}
", Color.SlateGray,
new Colorful.Formatter("https://github.com/2881099/FreeSql", Color.DeepSkyBlue),
new Colorful.Formatter("v" + string.Join(".", typeof(ConsoleApp).Assembly.GetName().Version.ToString().Split('.').Where((a, b) => b <= 2)), Color.SlateGray));

            ArgsNameOptions = new[] { false, false, false, false };
            ArgsNameSpace = "MyProject";
            ArgsFilter = "";
            ArgsMatch = "";
            Action<string> setArgsOutput = value =>
            {
                ArgsOutput = value;
                ArgsOutput = ArgsOutput.Trim().TrimEnd('/', '\\');
                ArgsOutput += ArgsOutput.Contains("\\") ? "\\" : "/";
                if (!Directory.Exists(ArgsOutput))
                    Directory.CreateDirectory(ArgsOutput);
            };
            setArgsOutput(Directory.GetCurrentDirectory());

            string args0 = args[0].Trim().ToLower();
            if (args[0] == "?" || args0 == "--help" || args0 == "-help")
            {

                Console.WriteFormatted(@"
    {0}

    更新工具：dotnet tool update -g FreeSql.Generator.LdfCore


  # 快速开始 #

  > {1} {2} 0,0,0,0 {3} MyProject {4} ""MySql,Data Source=127.0.0.1;...""
     
     -NameOptions              * 总共4个布尔值，分别对应：
                               # 首字母大写
                               # 首字母大写,其他小写
                               # 全部小写
                               # 下划线转驼峰
                               
     -NameSpace                * 项目名称
     
     -DB ""{5},Data Source=127.0.0.1;Port=3306;User ID=root;Password=root;Initial Catalog=数据库;Charset=utf8;SslMode=none;Max pool size=2""
     
     -DB ""{6},Data Source=.;Integrated Security=True;Initial Catalog=数据库;Pooling=true;Max Pool Size=2""
     
     -DB ""{7},Host=192.168.164.10;Port=5432;Username=postgres;Password=123456;Database=数据库;Pooling=true;Maximum Pool Size=2""
     
     -DB ""{8},user id=user1;password=123456;data source=//127.0.0.1:1521/XE;Pooling=true;Max Pool Size=2""

     -DB ""{9},Data Source=document.db""
     
     -DB ""{10},server=127.0.0.1;port=5236;user id=2user;password=123456789;database=2user;poolsize=2""
                               {10} 达梦数据库

     -DB ""{11},Driver={KingbaseES 8.2 ODBC Driver ANSI};Server=127.0.0.1;Port=54321;UID=USER2;PWD=123456789;database=数据库""
                               {11} 人大金仓数据库

     -DB ""{12},HOST=192.168.164.10;PORT=2003;DATABASE=OSRDB;USERNAME=SYSDBA;PASSWORD=szoscar55;MAXPOOLSIZE=2""
                               {12} 神舟通用数据库

     -Filter                   Table+View+StoreProcedure
                               默认生成：表+视图+存储过程
                               如果不想生成视图和存储过程 -Filter View+StoreProcedure

     -Match                    正则表达式，只生成匹配的表，如：dbo\.TB_.+

     -Output                   保存路径，默认为当前 shell 所在目录
                               {13}

", Color.SlateGray,
new Colorful.Formatter("使用 FreeSql 快速生成数据库的实体类和Services", Color.SlateGray),
new Colorful.Formatter("FreeSql.Generator.LdfCore", Color.White),
new Colorful.Formatter("-NameOptions", Color.ForestGreen),
new Colorful.Formatter("-NameSpace", Color.ForestGreen),
new Colorful.Formatter("-DB", Color.ForestGreen),
new Colorful.Formatter("MySql", Color.Yellow),
new Colorful.Formatter("SqlServer", Color.Yellow),
new Colorful.Formatter("PostgreSQL", Color.Yellow),
new Colorful.Formatter("Oracle", Color.Yellow),
new Colorful.Formatter("Sqlite", Color.Yellow),
new Colorful.Formatter("Dameng", Color.Yellow),
new Colorful.Formatter("OdbcKingbaseES", Color.Yellow),
new Colorful.Formatter("ShenTong", Color.Yellow),
new Colorful.Formatter("推荐在实体类目录创建 gen.bat，双击它重新所有实体类", Color.ForestGreen)
);
                wait.Set();
                return;
            }
            //输入判断
            for (int a = 0; a < args.Length; a++)
            {
                switch (args[a].Trim().ToLower())
                {

                    case "-nameoptions":
                        ArgsNameOptions = args[a + 1].Split(',').Select(opt => opt == "1").ToArray();
                        if (ArgsNameOptions.Length != 4) throw new ArgumentException("-NameOptions 参数错误，格式为：0,0,0,0");
                        a++;
                        break;
                    case "-namespace":
                        ArgsNameSpace = args[a + 1];
                        a++;
                        break;
                    case "-db":
                        var dbargs = args[a + 1].Split(',', 2);
                        if (dbargs.Length != 2) throw new ArgumentException("-DB 参数错误，格式为：MySql,ConnectionString");
                        switch (dbargs[0].Trim().ToLower())
                        {
                            case "mysql": ArgsDbType = DataType.MySql; break;
                            case "sqlserver": ArgsDbType = DataType.SqlServer; break;
                            case "postgresql": ArgsDbType = DataType.PostgreSQL; break;
                            case "oracle": ArgsDbType = DataType.Oracle; break;
                            case "sqlite": ArgsDbType = DataType.Sqlite; break;
                            case "dameng": ArgsDbType = DataType.Dameng; break;
                            case "kingbasees": ArgsDbType = DataType.KingbaseES; break;
                            case "shentong": ArgsDbType = DataType.ShenTong; break;
                            default: throw new ArgumentException($"-DB 参数错误，不支持的类型：\"{dbargs[0]}\"");
                        }
                        ArgsConnectionString = dbargs[1].Trim();
                        a++;
                        break;
                    case "-filter":
                        ArgsFilter = args[a + 1];
                        a++;
                        break;
                    case "-match":
                        ArgsMatch = args[a + 1];
                        if (Regex.IsMatch("", ArgsMatch)) { } //throw
                        a++;
                        break;
                    case "-output":
                        setArgsOutput(args[a + 1]);
                        a++;
                        break;
                    default:
                        throw new ArgumentException($"错误的参数设置：{args[a]}");
                }
            }

            if (string.IsNullOrEmpty(ArgsConnectionString)) throw new ArgumentException($"-DB 参数错误，未提供 ConnectionString");

            //RazorEngine设置
            RazorEngine.Engine.Razor = RazorEngineService.Create(new RazorEngine.Configuration.TemplateServiceConfiguration
            {
                EncodedStringFactory = new RazorEngine.Text.RawStringFactory(), // Raw string encoding.
            });

            //生成文件夹(打包)
            var baseFolder = ArgsOutput;
            //测试
            //var baseFolder = Path.Combine(ArgsOutput, "Temp");
            var domainsFolder = Path.Combine(baseFolder, "Domains");
            var queryFolder = Path.Combine(baseFolder, "Query");
            var dtoFolder = Path.Combine(baseFolder, "Dtos");
            var mapperFolder = Path.Combine(baseFolder, "Mapper");
            var servicesFolder = Path.Combine(baseFolder, "Services");
            var iservicesFolder = Path.Combine(baseFolder, "IServices");
            var controllerFolder = Path.Combine(baseFolder, "Controllers");
            var validatorFolder = Path.Combine(baseFolder, "Validators");

            //初始化模板
            var domainsTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/entity.cshtml"));
            var domainsTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(domainsTemplate, domainsTemplateId);

            var queryTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/query.cshtml"));
            var queryTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(queryTemplate, queryTemplateId);

            var inputDtoTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/inputdto.cshtml"));
            var inputDtoTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(inputDtoTemplate, inputDtoTemplateId);

            var outputDtoTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/outputdto.cshtml"));
            var outputDtoTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(outputDtoTemplate, outputDtoTemplateId);

            var listDtoTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/pagelist_outputdto.cshtml"));
            var listDtoTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(listDtoTemplate, listDtoTemplateId);

            var mapperTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/mapper.cshtml"));
            var mapperTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(mapperTemplate, mapperTemplateId);

            var servicesTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/services.cshtml"));
            var servicesTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(servicesTemplate, servicesTemplateId);

            var iservicesTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/iservices.cshtml"));
            var iservicesTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(iservicesTemplate, iservicesTemplateId);

            var controllerTemplate = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/controller.cshtml"));
            var controllerTemplateId = Guid.NewGuid().ToString("N");
            RazorEngine.Engine.Razor.Compile(controllerTemplate, controllerTemplateId);

            //校验器模板
            var validatorTemplateStr = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Templates/inputdto_validator.sbn"));
            var validatorTemplate = Template.Parse(validatorTemplateStr);
            if (validatorTemplate.HasErrors)
            {
                Console.WriteLine(validatorTemplate.Messages);
            }

            //使用FreeSql获取数据库信息，生成文件
            var outputCounter = 0;
            using (IFreeSql fsql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(ArgsDbType, ArgsConnectionString)
                .UseAutoSyncStructure(false)
                .UseMonitorCommand(cmd => Console.WriteFormatted(cmd.CommandText + "\r\n", Color.SlateGray))
                .Build())
            {
                var tables = fsql.DbFirst.GetTablesByDatabase();
                var outputTables = tables;
                //记录下来用于生成Mapper
                var allModels = new List<RazorModel>();
                //开始生成操作
                foreach (var table in outputTables)
                {
                    //过滤器判断
                    if (string.IsNullOrEmpty(ArgsMatch) == false)
                    {
                        if (Regex.IsMatch($"{table.Schema}.{table.Name}".TrimStart('.'), ArgsMatch) == false) continue;
                    }
                    //判断类型（表、试图、存储过程）
                    switch (table.Type)
                    {
                        case FreeSql.DatabaseModel.DbTableType.TABLE:
                            if (ArgsFilter.Contains("Table", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteFormatted(" Ignore Table -> " + table.Name + "\r\n", Color.DarkSlateGray);
                                continue;
                            }
                            break;
                        case FreeSql.DatabaseModel.DbTableType.VIEW:
                            if (ArgsFilter.Contains("View", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteFormatted(" Ignore View -> " + table.Name + "\r\n", Color.DarkSlateGray);
                                continue;
                            }
                            break;
                        case FreeSql.DatabaseModel.DbTableType.StoreProcedure:
                            if (ArgsFilter.Contains("StoreProcedure", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteFormatted(" Ignore StoreProcedure -> " + table.Name + "\r\n", Color.DarkSlateGray);
                                continue;
                            }
                            break;
                    }
                    //开始生成
                    var model = new RazorModel(fsql, ArgsNameSpace, ArgsNameOptions, tables, table);
                    //生成实体类
                    BuildAndWriteToFile(domainsTemplateId, model, Path.Combine(domainsFolder, model.GetModuleName(), $"{model.GetCsName(table.Name)}.cs"));
                    ++outputCounter;
                    //如果没有Id，说明是中间表，不生成其他文件
                    if (!model.columns.Select(a => model.GetCsName(a.Name)).Contains("Id"))
                    {
                        continue;
                    }
                    //生成列表查询参数模型
                    BuildAndWriteToFile(queryTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", "Query", $"{model.GetCsName(table.Name)}Query.cs"));
                    ++outputCounter;
                    //生成新增编辑参数模型
                    BuildAndWriteToFile(inputDtoTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", "Dtos", $"{model.GetCsName(table.Name)}Input.cs"));
                    ++outputCounter;
                    //生成详情输出模型
                    BuildAndWriteToFile(outputDtoTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", "Dtos", $"{model.GetCsName(table.Name)}Output.cs"));
                    ++outputCounter;
                    //生成列表输出模型
                    BuildAndWriteToFile(listDtoTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", "Dtos", $"{model.GetCsName(table.Name)}ListOutput.cs"));
                    ++outputCounter;
                    //生成服务接口
                    BuildAndWriteToFile(iservicesTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", $"I{model.GetCsName(table.Name)}Services.cs"));
                    ++outputCounter;
                    //生成服务类
                    BuildAndWriteToFile(servicesTemplateId, model, Path.Combine(servicesFolder, model.GetModuleName() + "Svc", $"{model.GetCsName(table.Name)}Services.cs"));
                    ++outputCounter;
                    //生成控制器类
                    BuildAndWriteToFile(controllerTemplateId, model, Path.Combine(controllerFolder, model.GetModuleName() + "Api", $"{model.GetCsName(table.Name)}Controller.cs"));
                    ++outputCounter;
                    //生成校验器
                    var scribanModel = new ScribanModel(model);
                    BuildAndWriteToFile(validatorTemplate, scribanModel, Path.Combine(validatorFolder, model.GetModuleName() + "", $"{model.GetCsName(table.Name)}InputValidator.cs"));
                    ++outputCounter;

                    allModels.Add(model);
                }
                foreach (var module in allModels.GroupBy(a => a.GetModuleName()))
                {
                    //生成Mapper类
                    BuildAndWriteToFile(mapperTemplateId, module.ToList(), Path.Combine(servicesFolder, module.Key + "Svc", "Profiles", $"MapperConfig.cs"));
                    ++outputCounter;
                }

            }
            //输出日志
            Console.WriteFormatted(" OUT Domains -> " + domainsFolder + "\r\n", Color.DeepSkyBlue);
            Console.WriteFormatted(" OUT Services -> " + servicesFolder + "\r\n", Color.DeepSkyBlue);
            Console.WriteFormatted(" OUT Controllers -> " + controllerFolder + "\r\n", Color.DeepSkyBlue);
            //生成bat
            var rebuildBat = Path.Combine(baseFolder, "__重新生成.bat");
            if (File.Exists(rebuildBat) == false)
            {
                File.WriteAllText(rebuildBat, $@"
FreeSql.Generator.LdfCore -NameOptions {string.Join(",", ArgsNameOptions.Select(a => a ? 1 : 0))} -NameSpace {ArgsNameSpace} -DB ""{ArgsDbType},{ArgsConnectionString}""{(string.IsNullOrEmpty(ArgsFilter) ? "" : $" -Filter \"{ArgsFilter}\"")}{(string.IsNullOrEmpty(ArgsMatch) ? "" : $" -Match \"{ArgsMatch}\"")}
");
                Console.WriteFormatted(" OUT -> " + rebuildBat + "    (以后) 双击它重新生成实体\r\n", Color.Magenta);
                ++outputCounter;
            }
            var folder = baseFolder;
            Console.WriteFormatted($"\r\n[{DateTime.Now.ToString("MM-dd HH:mm:ss")}] 生成完毕，总共生成了 {outputCounter} 个文件，目录：\"{folder}\"\r\n", Color.DarkGreen);

            Console.ReadKey();
            wait.Set();
        }

        /// <summary>
        /// 获取模块文件夹
        /// </summary>
        /// <returns></returns>
        private string GetModuleFolder(string baseFolder, RazorModel model)
        {
            return Path.Combine(baseFolder, model.GetModuleName());
        }

        private string GetFilePath(string baseFolder, RazorModel model, string suffix)
        {
            var module = GetModuleFolder(baseFolder, model);
            return Path.Combine(module, $"{model.GetCsName(model.table.Name)}{suffix}");
        }

        /// <summary>
        /// 生成文件
        /// </summary>
        /// <param name="razorId"></param>
        /// <param name="model"></param>
        /// <param name="filePath"></param>
        private void BuildAndWriteToFile(string razorId, RazorModel model, string filePath)
        {
            var sw = new StringWriter();
            RazorEngine.Engine.Razor.Run(razorId, sw, null, model);
            StringBuilder plus = new StringBuilder();
            plus.Append(sw.ToString());
            plus.AppendLine();

            var file = new FileInfo(filePath);
            file.Directory.Create();
            File.WriteAllText(file.FullName, plus.ToString());
            sw.Dispose();
        }

        /// <summary>
        /// 生成文件(Scriban模板)
        /// </summary>
        /// <param name="razorId"></param>
        /// <param name="model"></param>
        /// <param name="filePath"></param>
        private void BuildAndWriteToFile(Template template, ScribanModel model, string filePath)
        {
            var result = template.Render(model);
            var file = new FileInfo(filePath);
            file.Directory.Create();
            File.WriteAllText(file.FullName, result);
        }

        /// <summary>
        /// 生成文件(这里给生成Mapper用，其他地方暂时没用到)
        /// </summary>
        /// <param name="razorId"></param>
        /// <param name="model"></param>
        /// <param name="filePath"></param>
        private void BuildAndWriteToFile(string razorId, List<RazorModel> model, string filePath)
        {
            var sw = new StringWriter();
            RazorEngine.Engine.Razor.Run(razorId, sw, null, model);
            StringBuilder plus = new StringBuilder();
            plus.Append(sw.ToString());
            plus.AppendLine();

            var file = new FileInfo(filePath);
            file.Directory.Create();
            File.WriteAllText(file.FullName, plus.ToString());
            sw.Dispose();
        }
    }
}

