using System.Reflection;

using Newtonsoft.Json;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace StoicGoose.Debugging
{
	public sealed class Breakpoint
	{
		readonly static ScriptOptions scriptOptions = ScriptOptions.Default.AddReferences(Assembly.GetExecutingAssembly());

		readonly static string codeDummy = $"default({typeof(bool).FullName})";

		static Script<bool> lastScriptState = default;

		public string Expression = string.Empty;
		public bool Enabled = true;
		[JsonIgnore()]
		public ScriptRunner<bool> Runner = null;

		static Breakpoint() => lastScriptState = CSharpScript.Create<bool>(codeDummy, scriptOptions, typeof(BreakpointVariables));

		public bool UpdateDelegate()
		{
			try
			{
				if (string.IsNullOrEmpty(Expression)) return false;
				var newScriptState = lastScriptState.ContinueWith<bool>($"return {Expression};", scriptOptions);
				Runner = newScriptState.CreateDelegate();
				lastScriptState = newScriptState;
			}
			catch (CompilationErrorException)
			{
				return false;
			}
			catch
			{
				throw;
			}

			return true;
		}
	}
}
