using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace GenerateAst {
	class Program {
		static int Main(string[] args) {

			string startingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			DirectoryInfo di = new DirectoryInfo(startingPath);
			var path = di.Parent.Parent.Parent.FullName;
			var outputPath = Path.Combine(path, "CsLox");

			Dictionary<string, string> _expr_ast = new Dictionary<string, string> {
				{ "Assign", "Token name, Expr value" },
				{ "Binary", "Expr left, Token op, Expr right" },
				{ "Call", "Expr callee, Token paren, List<Expr> arguments" },
				{ "Get", "Expr obj, Token name" },
				{ "Set", "Expr obj, Token name, Expr value" },
				{ "Super", "Token keyword, Token method" },
				{ "This", "Token keyword" },
				{ "Grouping", "Expr expression" },
				{ "Logical", "Expr left, Token op, Expr right" },
				{ "Literal", "Object value" },
				{ "Unary", "Token op, Expr right" },
				{ "Variable", "Token name" },
			};

			GenerateFile(Path.Combine(outputPath, "Gen_Expr.cs"), "Expr", _expr_ast);

			Dictionary<string, string> _stmt_ast = new Dictionary<string, string> {
				{ "Block", "List<Stmt> statements" },
				{ "Class", "Token name, VariableExpr superclass, List<FunctionStmt> methods" },
				{ "Expression", "Expr expression"},
				{ "Function", "Token name, List<Token> parameters, List<Stmt> body" },
				{ "If", "Expr condition, Stmt thenBranch, Stmt elseBranch" },
				{ "Print", "Expr expression"},
				{ "Return", "Token keyword, Expr value" },
				{ "Var", "Token name, Expr initializer"},
				{ "While", "Expr condition, Stmt body" },
			};

			GenerateFile(Path.Combine(outputPath, "Gen_Stmt.cs"), "Stmt", _stmt_ast);

			return 0;
		}

		static void GenerateFile(string outputPath, string baseClassName, Dictionary<string, string> ast) {
			StringBuilder contents = new StringBuilder();

			// Using & Namespace
			contents.Append("using System;\n");
			contents.Append("using System.Collections.Generic;\n\n");
			contents.Append("namespace CsLox {\n\n");

			// Visitor Interface
			string visitorName = "Visitor_" + baseClassName;
			contents.Append("interface " + visitorName + "<R> {\n");
			foreach(var item in ast) {
				contents.Append("\tR Visit_" + item.Key + baseClassName + "(" + item.Key + baseClassName + " " + baseClassName.ToLower() + ");\n");
			}
			contents.Append("}\n\n");

			// Base Class
			contents.Append("abstract class " + baseClassName + " {\n");
			contents.Append("\tpublic abstract R Accept<R>(" + visitorName + "<R> visitor);\n");
			contents.Append("}\n\n");

			// Subclasses
			foreach(var item in ast) {
				string className = item.Key;
				contents.Append("class " + className + baseClassName + " : " + baseClassName + " {\n");

				// Fields
				string[] fields = item.Value.Split(',');
				foreach(var field in fields) {
					contents.Append("\tpublic readonly " + field.Trim() + ";\n");
				}
				contents.Append('\n');

				// CTor
				contents.Append("\tpublic " + className + baseClassName + " (");

				// Arguments
				foreach(var field in fields) {
					contents.Append(field.Trim()).Append(',');
				}
				if(fields.Length > 0) contents.Length -= 1; // Backtrack last ,
				contents.Append(") {\n");

				// Assignments
				foreach(var field in fields) {
					var fieldName = field.Trim().Split(' ')[1];
					contents.Append("\t\tthis." + fieldName + " = " + fieldName + ";\n");
				}

				// Close CTor
				contents.Append("\t}\n");
				contents.Append('\n');

				// Accept visitor
				contents.Append("\tpublic override R Accept<R>(" + visitorName + "<R> visitor) {\n");
				contents.Append("\t\treturn visitor.Visit_" + className + baseClassName + "(this);\n");
				contents.Append("\t}\n");

				// Close class
				contents.Append("}\n\n");
			}
			contents.Append("}\n");

			File.WriteAllText(outputPath, contents.ToString());
		}
	}
}


