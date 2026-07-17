using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using IF.Batch.Common.Configuration;
using IF.Batch.Common.Diagnostics;
using IF.Batch.Common.Helper;
using IF.Batch.Common.Service;
using IF.Batch.DTFX.Executors;
using IF.Batch.DTFX.Exceptions;
using IF.Batch.DTFX.Helper;
using IF.Batch.DTFX.Service;

namespace DTFX.SmokeTests
{
    internal static class Program
    {
        private static int Main()
        {
            var failures = new List<string>();
            Run("result priority", TestResultPriority, failures);
            Run("argument parsing", TestArgumentParsing, failures);
            Run("argument parsing edge cases", TestArgumentParsingEdgeCases, failures);
            Run("CSV formatting", TestCsvFormatting, failures);
            Run("CSV enumerable is read once", TestCsvEnumerableIsReadOnce, failures);
            Run("expression evaluation", TestExpressionEvaluation, failures);
            Run("executor factory mappings", TestExecutorFactoryMappings, failures);
            Run("executor factory injection", TestExecutorFactoryInjection, failures);
            Run("nested executor factory injection", TestNestedExecutorFactoryInjection, failures);
            Run("data transfer service injection", TestDataTransferServiceInjection, failures);
            Run("trace logger injection", TestTraceLoggerInjection, failures);
            Run("executor logger propagation", TestExecutorLoggerPropagation, failures);
            Run("data transfer context logger injection", TestDataTransferContextLoggerInjection, failures);
            Run("Serilog file logging", TestSerilogFileLogging, failures);
            Run("XSD and examples", TestSchemasAndExamples, failures);

            if (failures.Count == 0)
            {
                Console.WriteLine("All DTFX smoke tests passed.");
                return 0;
            }

            foreach (string failure in failures)
            {
                Console.Error.WriteLine(failure);
            }
            return 1;
        }

        private static void Run(string name, Action test, ICollection<string> failures)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception ex)
            {
                failures.Add("FAIL: " + name + " - " + ex.Message);
            }
        }

        private static void TestResultPriority()
        {
            var executor = new ResultExecutor();
            AssertEqual(ResultTypeCode.Error, executor.Merge(ResultTypeCode.Warning, ResultTypeCode.Error));
            AssertEqual(ResultTypeCode.Warning, executor.Merge(ResultTypeCode.Success, ResultTypeCode.Warning));
            AssertEqual(ResultTypeCode.Success, executor.Merge(ResultTypeCode.Success));
        }

        private static void TestArgumentParsing()
        {
            var arguments = new InputArguments(new[] { "-appid", "QUICKSTART", "-appdirectory", "." });
            AssertEqual("QUICKSTART", arguments["appid"]);
            AssertEqual(".", arguments["appdirectory"]);
        }

        private static void TestArgumentParsingEdgeCases()
        {
            var arguments = new InputArguments(new[] { "-APPID", "first", "-appid", "last", "-dry-run", null });

            AssertEqual("last", arguments["appid"]);
            AssertEqual("last", arguments["-APPID"]);
            AssertEqual(true, arguments.Contains("dry-run"));
            AssertEqual<string>(null, arguments["dry-run"]);
        }

        private static void TestCsvFormatting()
        {
            var defaultFormatter = new CsvFormatter();
            AssertEqual("plain,\"with,comma\",\"quoted \"\"value\"\"\",\" line \"",
                defaultFormatter.ToCsv(new[] { "plain", "with,comma", "quoted \"value\"", " line " }).ToString());

            var semicolonFormatter = new CsvFormatter(";");
            AssertEqual("\"with;semicolon\";with,comma",
                semicolonFormatter.ToCsv(new[] { "with;semicolon", "with,comma" }).ToString());

            var trimmingFormatter = new CsvFormatter(trimWhiteSpace: true);
            AssertEqual("value,", trimmingFormatter.ToCsv(new[] { " value ", null }).ToString());
        }

        private static void TestCsvEnumerableIsReadOnce()
        {
            var formatter = new CsvFormatter();
            var fields = new SingleUseEnumerable(new object[] { "first", 2, null });

            AssertEqual("first,2,", formatter.ToCsv(fields).ToString());
        }

        private static void TestExpressionEvaluation()
        {
            var evaluator = new ExpressionEvaluator();

            AssertEqual("true", evaluator.Evaluate("1 + 1 == 2"));
            AssertEqual("true", evaluator.Evaluate("'HELLOWORLD' == 'HELLOWORLD' && ('OTHER' != 'HELLOWORLD')"));
            AssertEqual("false", evaluator.Evaluate("10 < 2 || false"));
            AssertEqual("7", evaluator.Evaluate("1 + 2 * 3"));
            AssertEqual("fallback", evaluator.Evaluate("false ? 'selected' : 'fallback'"));
            AssertEvaluationFails(evaluator, "new ActiveXObject('WScript.Shell')");
        }

        private static void TestExecutorFactoryMappings()
        {
            var factory = new ExecutorFactory();
            using (var context = new DataTransferContext())
            {
                ExecutorBase sqlExecutor = factory.CreateExecutor(new XElement("SqlSelect"), context);
                AssertEqual(typeof(SqlSelectExecutor), sqlExecutor.GetType());
                AssertSame(context, sqlExecutor.ServiceContext);

                ExecutorBase ifExecutor = factory.CreateExecutor(new XElement("If"), context);
                AssertEqual(typeof(IfExecutor), ifExecutor.GetType());
                AssertSame(context, ifExecutor.ServiceContext);

                try
                {
                    factory.CreateExecutor(new XElement("Unsupported"), context);
                }
                catch (AppConfigurationException)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Expected an unsupported XML element to be rejected.");
        }

        private static void TestExecutorFactoryInjection()
        {
            var factory = new RecordingExecutorFactory(
                ResultTypeCode.Success,
                ResultTypeCode.Warning,
                ResultTypeCode.Error);

            using (var context = new DataTransferContext())
            {
                var executor = new ApplicationExecutor(factory) { ServiceContext = context };
                var application = new XElement("Application",
                    new XElement("First"),
                    new XElement("Second"),
                    new XElement("Third"));

                AssertEqual(ResultTypeCode.Error, executor.Execute(application));
                AssertEqual("First,Second,Third", string.Join(",", factory.ExecutedElementNames));
                AssertEqual(3, factory.ServiceContexts.Count);
                foreach (DataTransferContext serviceContext in factory.ServiceContexts)
                {
                    AssertSame(context, serviceContext);
                }
            }
        }

        private static void TestNestedExecutorFactoryInjection()
        {
            var factory = new RecordingExecutorFactory(ResultTypeCode.Warning);
            using (var context = new DataTransferContext())
            {
                var executor = new IfExecutor(factory) { ServiceContext = context };
                var ifElement = new XElement("If",
                    new XAttribute("test", "true"),
                    new XElement("Nested"));

                AssertEqual(ResultTypeCode.Warning, executor.Execute(ifElement));
                AssertEqual("Nested", string.Join(",", factory.ExecutedElementNames));
                AssertSame(context, factory.ServiceContexts[0]);
            }
        }

        private static void TestDataTransferServiceInjection()
        {
            var context = new DataTransferContext
            {
                RootElement = new XElement("Application", new XElement("Injected"))
            };
            var contextFactory = new StubDataTransferContextFactory(context, true);
            var executorFactory = new RecordingExecutorFactory(ResultTypeCode.Warning);
            var service = new DataTransferService(contextFactory, executorFactory);

            AssertEqual(true, service.EnsureServiceConfigurations());
            AssertSame(context, service.ServiceContext);
            AssertEqual(true, service.InitService());

            service.ExecuteService();
            AssertEqual(ResultTypeCode.Warning, service.ServiceResult);
            AssertEqual("Injected", string.Join(",", executorFactory.ExecutedElementNames));
            AssertEqual(1, contextFactory.CreateCount);

            service.Dispose();
            service.Dispose();
            try
            {
                service.InitService();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            throw new InvalidOperationException("Expected a disposed service to reject initialization.");
        }

        private static void TestTraceLoggerInjection()
        {
            var context = new DataTransferContext
            {
                RootElement = new XElement("Application", new XElement("Unsupported"))
            };
            var contextFactory = new StubDataTransferContextFactory(context, true);
            var logger = new RecordingTraceLogger();

            using (var service = new DataTransferService(
                contextFactory,
                new ExecutorFactory(),
                logger))
            {
                AssertEqual(true, service.EnsureServiceConfigurations());
                service.ExecuteService();

                AssertEqual(ResultTypeCode.Error, service.ServiceResult);
                AssertEqual(1, logger.Errors.Count);
                AssertContains(logger.Errors[0], "Unsupported");
            }
        }

        private static void TestExecutorLoggerPropagation()
        {
            var logger = new RecordingTraceLogger();
            var factory = new ExecutorFactory(logger);

            using (var context = new DataTransferContext())
            {
                var application = new XElement("Application",
                    new XElement("If",
                        new XAttribute("test", "true"),
                        new XElement("TraceLog",
                            new XAttribute("eventType", "information"),
                            "nested logger")));
                ITaskExecutor<XElement> executor = factory.CreateApplicationExecutor(context);

                AssertEqual(ResultTypeCode.Success, executor.Execute(application));
                AssertEqual(1, logger.Infos.Count);
                AssertEqual("nested logger", logger.Infos[0]);
            }
        }

        private static void TestDataTransferContextLoggerInjection()
        {
            var logger = new RecordingTraceLogger();

            using (var context = new DataTransferContext(logger))
            {
                context.CommitAllTransactions();
                context.RollbackAllTransactions();
            }

            AssertEqual(2, logger.Debugs.Count);
            AssertContains(logger.Debugs[0], "コミット");
            AssertContains(logger.Debugs[1], "ロールバック");
        }

        private static void AssertEvaluationFails(ExpressionEvaluator evaluator, string expression)
        {
            try
            {
                evaluator.Evaluate(expression);
            }
            catch (Exception)
            {
                return;
            }

            throw new InvalidOperationException("Expected expression to be rejected: " + expression);
        }

        private static void TestSerilogFileLogging()
        {
            string directory = Path.Combine(Path.GetTempPath(), "dtfx-serilog-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "trace_%YYYYMMDD%.log");
            Directory.CreateDirectory(directory);
            try
            {
                using (var writer = new SerilogTraceLogWriter())
                {
                    writer.Initialize(new TestTraceLogConfiguration(path));
                    writer.WriteTrace(System.Diagnostics.TraceEventType.Information,
                        "DTFX.SmokeTests.Program.TestSerilogFileLogging", "message,with comma");
                    writer.WriteException(new InvalidOperationException("expected failure"), "operation failed");
                }

                string resolvedPath = FileHelper.ResolvePathFromTemplate(path, DateTime.Now);
                string contents = File.ReadAllText(resolvedPath);
                AssertContains(contents, "Information");
                AssertContains(contents, "DTFX.SmokeTests.Program.TestSerilogFileLogging");
                AssertContains(contents, "\"message,with comma\"");
                AssertContains(contents, "InvalidOperationException");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private static void TestSchemasAndExamples()
        {
            string root = FindRepositoryRoot();
            string schemaPath = Path.Combine(root, "DTFX", "XMLSchema", "Application.xsd");
            var schemas = new XmlSchemaSet();
            schemas.Add(null, schemaPath);
            schemas.Compile();

            ValidateXml(Path.Combine(root, "examples", "quickstart", "QUICKSTART.XML"), schemas);
            ValidateXml(Path.Combine(root, "examples", "csv-pipeline", "CSV_PIPELINE.XML"), schemas);
            ValidateXml(Path.Combine(root, "DTFX", "01_バッチ作成サンプル", "SAMPLE_APP.XML"), schemas);
        }

        private static void ValidateXml(string path, XmlSchemaSet schemas)
        {
            XDocument document = XDocument.Load(path);
            document.Validate(schemas, delegate(object sender, ValidationEventArgs args)
            {
                throw new XmlSchemaValidationException(path + ": " + args.Message, args.Exception);
            });
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "IF.Batch.sln")))
            {
                directory = directory.Parent;
            }
            if (directory == null)
            {
                throw new DirectoryNotFoundException("Could not locate IF.Batch.sln.");
            }
            return directory.FullName;
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
            }
        }

        private static void AssertContains(string value, string expected)
        {
            if (value == null || value.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Expected value to contain '" + expected + "'.");
            }
        }

        private static void AssertSame(object expected, object actual)
        {
            if (!object.ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException("Expected both values to reference the same object.");
            }
        }

        private sealed class TestTraceLogConfiguration : ITraceLogConfiguration
        {
            private readonly string _path;

            public TestTraceLogConfiguration(string path)
            {
                _path = path;
            }

            public string TracePathTemplate { get { return _path; } }
            public System.Diagnostics.SourceLevels TraceSourceLevels { get { return System.Diagnostics.SourceLevels.All; } }
            public bool AutoFlush { get { return true; } }
            public long MaxSize { get { return 1024 * 1024; } }
            public int BufferSize { get { return 8192; } }
            public System.Text.Encoding Encoding { get { return System.Text.Encoding.UTF8; } }
            public bool UseGzip { get { return false; } }
            public bool Append { get { return true; } }
        }

        private sealed class ResultExecutor : ExecutorBase
        {
            public ResultTypeCode Merge(params ResultTypeCode[] codes)
            {
                return MergeResultTypeCode(codes);
            }

            public override ResultTypeCode Execute(XElement parameter)
            {
                return ResultTypeCode.Success;
            }
        }

        private sealed class RecordingExecutorFactory : IExecutorFactory
        {
            private readonly Queue<ResultTypeCode> _results;

            public RecordingExecutorFactory(params ResultTypeCode[] results)
            {
                _results = new Queue<ResultTypeCode>(results);
                ExecutedElementNames = new List<string>();
                ServiceContexts = new List<DataTransferContext>();
            }

            public IList<string> ExecutedElementNames { get; private set; }

            public IList<DataTransferContext> ServiceContexts { get; private set; }

            public ITaskExecutor<XElement> CreateApplicationExecutor(DataTransferContext serviceContext)
            {
                return new ApplicationExecutor(this) { ServiceContext = serviceContext };
            }

            public ExecutorBase CreateExecutor(XElement element, DataTransferContext serviceContext)
            {
                if (_results.Count == 0)
                {
                    throw new InvalidOperationException("No executor result was configured.");
                }

                var executor = new RecordingExecutor(
                    _results.Dequeue(),
                    delegate (XElement executedElement)
                    {
                        ExecutedElementNames.Add(executedElement.Name.LocalName);
                        ServiceContexts.Add(serviceContext);
                    });
                executor.ServiceContext = serviceContext;
                return executor;
            }
        }

        private sealed class RecordingExecutor : ExecutorBase
        {
            private readonly ResultTypeCode _result;
            private readonly Action<XElement> _onExecute;

            public RecordingExecutor(ResultTypeCode result, Action<XElement> onExecute)
            {
                _result = result;
                _onExecute = onExecute;
            }

            public override ResultTypeCode Execute(XElement parameter)
            {
                _onExecute(parameter);
                return _result;
            }
        }

        private sealed class StubDataTransferContextFactory : IDataTransferContextFactory
        {
            private readonly DataTransferContext _context;
            private readonly bool _result;

            public StubDataTransferContextFactory(DataTransferContext context, bool result)
            {
                _context = context;
                _result = result;
            }

            public int CreateCount { get; private set; }

            public bool TryCreate(out DataTransferContext context)
            {
                CreateCount++;
                context = _context;
                return _result;
            }
        }

        private sealed class RecordingTraceLogger : ITraceLogger
        {
            public RecordingTraceLogger()
            {
                Infos = new List<string>();
                Debugs = new List<string>();
                Errors = new List<string>();
                Exceptions = new List<Exception>();
            }

            public IList<string> Infos { get; private set; }

            public IList<string> Debugs { get; private set; }

            public IList<string> Errors { get; private set; }

            public IList<Exception> Exceptions { get; private set; }

            public void WriteInfo(System.Reflection.MethodBase method, string message, params object[] args)
            {
                Infos.Add(FormatMessage(message, args));
            }

            public void WriteWarning(System.Reflection.MethodBase method, string message, params object[] args)
            {
            }

            public void WriteError(System.Reflection.MethodBase method, string message, params object[] args)
            {
                Errors.Add(FormatMessage(message, args));
            }

            public void WriteDebug(System.Reflection.MethodBase method, string message, params object[] args)
            {
                Debugs.Add(FormatMessage(message, args));
            }

            public void WriteException(Exception exception, string appendMessage = null)
            {
                Exceptions.Add(exception);
            }

            private static string FormatMessage(string message, object[] args)
            {
                return args == null || args.Length == 0
                    ? message
                    : string.Format(message, args);
            }
        }

        private sealed class SingleUseEnumerable : IEnumerable<object>
        {
            private readonly IEnumerable<object> _values;
            private bool _wasEnumerated;

            public SingleUseEnumerable(IEnumerable<object> values)
            {
                _values = values;
            }

            public IEnumerator<object> GetEnumerator()
            {
                if (_wasEnumerated)
                {
                    throw new InvalidOperationException("The sequence was enumerated more than once.");
                }

                _wasEnumerated = true;
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
