using Microsoft.Build.Framework;

namespace BuildTask
{
    public abstract class TaskBase : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

        protected bool HasLoggedErrors { get; private set; }

        public bool Execute()
        {
            ExecuteTask();

            return !HasLoggedErrors;
        }

        protected abstract void ExecuteTask();

        protected void LogError(string message, string code = null, bool includeLocation = false)
        {
            HasLoggedErrors = true;

            BuildEngine?.LogErrorEvent(new BuildErrorEventArgs(
                subcategory: null,
                code: code,
                file: includeLocation ? BuildEngine?.ProjectFileOfTaskNode : null,
                lineNumber: includeLocation ? (int)BuildEngine?.LineNumberOfTaskNode : 0,
                columnNumber: includeLocation ? (int)BuildEngine?.ColumnNumberOfTaskNode : 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null));
        }

        protected void LogMessage(string message, MessageImportance importance = MessageImportance.Normal)
        {
            BuildEngine?.LogMessageEvent(new BuildMessageEventArgs(
                subcategory: null,
                code: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null,
                importance: importance));
        }

        protected void LogWarning(string message, string code = null, bool includeLocation = false)
        {
            BuildEngine?.LogWarningEvent(new BuildWarningEventArgs(
                subcategory: null,
                code: code,
                file: includeLocation ? BuildEngine?.ProjectFileOfTaskNode : null,
                lineNumber: includeLocation ? (int) BuildEngine?.LineNumberOfTaskNode : 0,
                columnNumber: includeLocation ? (int) BuildEngine?.ColumnNumberOfTaskNode : 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                message: message,
                helpKeyword: null,
                senderName: null));
        }
    }
}