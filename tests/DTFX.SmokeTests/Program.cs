using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using IF.Batch.Common.Configuration;
using IF.Batch.Common.Helper;
using IF.Batch.DTFX.Executors;

namespace DTFX.SmokeTests
{
    internal static class Program
    {
        private static int Main()
        {
            var failures = new List<string>();
            Run("result priority", TestResultPriority, failures);
            Run("argument parsing", TestArgumentParsing, failures);
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

        private static void TestSchemasAndExamples()
        {
            string root = FindRepositoryRoot();
            string schemaPath = Path.Combine(root, "DTFX", "XMLSchema", "Application.xsd");
            var schemas = new XmlSchemaSet();
            schemas.Add(null, schemaPath);
            schemas.Compile();

            ValidateXml(Path.Combine(root, "examples", "quickstart", "QUICKSTART.XML"), schemas);
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
    }
}
