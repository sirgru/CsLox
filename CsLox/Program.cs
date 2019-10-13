using System;
using System.Diagnostics;
using System.IO;

namespace CsLox {
	class Program {
		private static Interpreter _interpreter = new Interpreter();
		private static bool _debugScanner;
		private static bool _debugParser;
		private static bool _timedExecution;
		private static bool _timedAll;

		static int Main(string[] args) {

			if(args.Length == 0) {
				return RunPrompt();

			} else if(args.Length == 1) {
				if(args[0] == "-h") ShowHelp();
				return RunFile(args[0]);

			} else {
				for(int i = 1; i < args.Length; i++) {
					if(args[i] != "-t" && args[i] != "-ta" && args[i] != "-ds" && args[i] != "-dp") {
						Console.WriteLine("Invalid argument supplied.");
						return 64;
					}
				}
				for(int i = 1; i < args.Length; i++) {
					if(args[i] == "-t") _timedExecution = true;
					if(args[i] == "-ta") _timedAll = true;
					if(args[i] == "-ds") _debugScanner = true;
					if(args[i] == "-dp") _debugParser = true;
				}
				return RunFile(args[0]);
			}
		}
		static void ShowHelp() {
			Console.WriteLine("Use without arguments to enter interpretermode.");
			Console.WriteLine("Use only -h argument to display help.");
			Console.WriteLine("Use only one path argument to run file at path.");
			Console.WriteLine("Use only -t argument to time interpretation.");
			Console.WriteLine("Use only -ta argument to time all execution stages.");
			Console.WriteLine("Use only -ds argument to time debug scanner.");
			Console.WriteLine("Use only -dp argument to time debug parser.");
		}

		static int RunFile(String path) {
			if(!File.Exists(path)) {
				Console.WriteLine("File not found.");
				return 64;
			}
			var contents = File.ReadAllText(path);
			return Run(contents);
		}

		static int RunPrompt() {
			String inputString = "";
			String inputLine;
			Console.WriteLine("Welcome to C# Lox.\nInput ;; to end input mode.");
			Console.Write("> ");
			while((inputLine = Console.ReadLine().Trim()) != ";;") {
				Console.Write("> ");
				inputString += inputLine + "\n";
			}
			return Run(inputString);
		}

		static int Run(String source) {
			Stopwatch sw = null;
			if(_timedAll) {
				sw = new Stopwatch();
				sw.Start();
			}

			var scanner = new Scanner(source);
			var scannerResult = scanner.ScanTokens();

			if(_timedAll) {
				sw.Stop();
				Console.WriteLine("Scanner (ms):" + sw.ElapsedMilliseconds);
				sw.Reset();
			}

			if(_debugScanner) {
				Console.WriteLine(source);
				Console.WriteLine();
				foreach(var token in scannerResult.tokens) {
					Console.WriteLine(token.Debug());
				}
			}

			if(scannerResult.errors.Count != 0) {
				AnnounceErrors();
				var errors = ErrorHelpers.ScannerErrorsMergeInvalidCharacters(scannerResult.errors);
				foreach(var error in errors) {
					ReportError(source, error);
				}
				return 65;
			}

			if(_timedAll) {
				sw.Start();
			}

			Parser parser = new Parser(scannerResult.tokens);
			ParseResult parseResult = parser.Parse();

			if(_timedAll) {
				sw.Stop();
				Console.WriteLine("Parser (ms):" + sw.ElapsedMilliseconds);
				sw.Reset();
			}

			if(parseResult.errors.Count != 0) {
				foreach(var error in parseResult.errors) {
					ReportError(source, error);
				}
				return 65;
			}

			if(_debugParser) {
				int i = 1;
				foreach(var stmt in parseResult.statements) {
					Console.Write(i + ": ");
					Console.WriteLine(new AST_Printer().Print(stmt));
					i++;
				}
			}

			if(_timedAll) {
				sw.Start();
			}

			ScopeResolver resolver = new ScopeResolver(_interpreter);
			var resolverResult = resolver.StartResolve(parseResult.statements);

			if(_timedAll) {
				sw.Stop();
				Console.WriteLine("Analyzer (ms):" + sw.ElapsedMilliseconds);
				sw.Reset();
			}

			if(resolverResult.Count > 0) {
				foreach(var error in resolverResult) {
					ReportError(source, error);
				}
				return 65;
			}

			if(_timedAll || _timedExecution) {
				if(sw == null) sw = new Stopwatch();
				sw.Start();
			}

			var interpretError = _interpreter.Interpret(parseResult.statements);

			if(_timedAll || _timedExecution) {
				sw.Stop();
				Console.WriteLine("Interpreter (ms):" + sw.ElapsedMilliseconds);
				sw.Reset();
			}

			if(interpretError != null) {
				ReportRuntimeError(interpretError);
				return 70;
			}

			return 0;
		}

		static void AnnounceErrors() {
			Console.WriteLine("Errors Found:\n");
		}

		static void ReportError(String source, Error error) {
			var lines = source.Split('\n');
			var errorLine = lines[error.lineStart - 1].Replace('\t', ' ');
			String preamble = error.lineStart.ToString() + "| ";
			Console.WriteLine(preamble + errorLine);
			Console.Write(new String(' ', preamble.Length + error.columnStart - 1));
			for(int i = error.columnStart; i <= error.columnEnd; i++) {
				Console.Write("^");
			}
			Console.WriteLine("--- " + error);
		}

		static void ReportRuntimeError(string message) {
			Console.WriteLine(message);
			Console.WriteLine();
		}

		static void DisplayResult(string result) {
			Console.WriteLine(result);
			Console.WriteLine();
		}
	}
}
