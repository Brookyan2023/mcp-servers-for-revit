using System.IO;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _generatedCode;
        private object[] _executionParameters;
        private string _transactionMode = "auto";

        public ExecutionResultInfo ResultInfo { get; private set; }

        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetExecutionParameters(string code, object[] parameters = null, string transactionMode = "auto")
        {
            _generatedCode = code;
            _executionParameters = parameters ?? Array.Empty<object>();
            _transactionMode = string.IsNullOrWhiteSpace(transactionMode)
                ? "auto"
                : transactionMode.Trim().ToLowerInvariant();
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    throw new InvalidOperationException("No active Revit document.");
                }

                ResultInfo = new ExecutionResultInfo();

                if (ShouldUseTransaction(_transactionMode))
                {
                    using (var transaction = new Transaction(doc, "Execute AI Code"))
                    {
                        transaction.Start();

                        var result = CompileAndExecuteCode(
                            code: _generatedCode,
                            app: app,
                            doc: doc,
                            parameters: _executionParameters
                        );

                        transaction.Commit();

                        ResultInfo.Success = true;
                        ResultInfo.Result = JsonConvert.SerializeObject(result);
                    }
                }
                else
                {
                    var result = CompileAndExecuteCode(
                        code: _generatedCode,
                        app: app,
                        doc: doc,
                        parameters: _executionParameters
                    );

                    ResultInfo.Success = true;
                    ResultInfo.Result = JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex)
            {
                ResultInfo.Success = false;
                ResultInfo.ErrorMessage = $"Execution failed: {UnwrapException(ex).Message}";
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private static bool ShouldUseTransaction(string transactionMode)
        {
            return transactionMode == "transaction" || transactionMode == "auto";
        }

        private static Exception UnwrapException(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }

        private object CompileAndExecuteCode(string code, UIApplication app, Document doc, object[] parameters)
        {
            var wrappedCode = $@"
using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace AIGeneratedCode
{{
    public static class CodeExecutor
    {{
        public static object Execute(UIApplication uiapp, Document document, object[] parameters)
        {{
            var uiApp = uiapp;
            var uidoc = uiapp.ActiveUIDocument;
            var uiDoc = uidoc;
            var doc = document;
            var Document = document;
            var activeView = document.ActiveView;

            {code}
        }}
    }}
}}";

            var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                "AIGeneratedCode",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var errors = string.Join("\n", result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => $"Line {d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}"));
                    throw new Exception($"Code compilation error:\n{errors}");
                }

                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                return executeMethod.Invoke(null, new object[] { app, doc, parameters });
            }
        }

        public string GetName()
        {
            return "Execute AI Code";
        }
    }

    public class ExecutionResultInfo
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
