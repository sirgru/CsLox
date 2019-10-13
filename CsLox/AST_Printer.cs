using System;
using System.Collections.Generic;
using System.Text;

namespace CsLox {
	class AST_Printer : Visitor_Expr<String>, Visitor_Stmt<String> {
		public String Print(Expr expr) {
			return expr.Accept(this);
		}
		public String Print(Stmt stmt) {
			return stmt.Accept(this);
		}

		public string Visit_AssignExpr(AssignExpr expr) {
			return Parenthesize("Assign " + (string)expr.name.value, expr.value);
		}

		public string Visit_BinaryExpr(BinaryExpr expr) {
			return Parenthesize(expr.op.ToText(), expr.left, expr.right);
		}

		public string Visit_BlockStmt(BlockStmt stmt) {
			return Parenthesize("Block", stmt.statements.ToArray());
		}

		public string Visit_CallExpr(CallExpr expr) {
			var exprs = new List<Expr>();
			exprs.Add(expr.callee);
			exprs.AddRange(expr.arguments);
			return Parenthesize("FnCall", exprs.ToArray());
		}

		public string Visit_ClassStmt(ClassStmt stmt) {
			var name = (string)stmt.name.value;
			return Parenthesize("ClassDecl " + name, stmt.methods.ToArray());
		}

		public string Visit_ExpressionStmt(ExpressionStmt stmt) {
			return Parenthesize("Stmt", stmt.expression);
		}

		public string Visit_FunctionStmt(FunctionStmt stmt) {
			StringBuilder sb = new StringBuilder();
			sb.Append("(FnDecl ");
			sb.Append("'" + (string)stmt.name.value + "'");
			sb.Append(' ');
			foreach(var item in stmt.parameters) {
				sb.Append("'" + (string)item.value + "'");
				sb.Append(' ');
			}
			sb.Append(Parenthesize("Body", stmt.body.ToArray()));
			sb.Append(')');
			return sb.ToString();
		}

		public string Visit_GetExpr(GetExpr expr) {
			var name = (string) expr.name.value;
			return Parenthesize("Get " + name, expr.obj);
		}

		public string Visit_GroupingExpr(GroupingExpr expr) {
			return Parenthesize("Group", expr.expression);
		}

		public string Visit_IfStmt(IfStmt stmt) {
			if(stmt.elseBranch != null) {
				return Parenthesize("If", stmt.thenBranch, stmt.elseBranch);
			} else {
				return Parenthesize("If", stmt.thenBranch);
			}
		}

		public string Visit_LiteralExpr(LiteralExpr expr) {
			if(expr.value == null) return "nil";
			return "'" + expr.value.ToString() + "'";
		}

		public string Visit_LogicalExpr(LogicalExpr expr) {
			return Parenthesize(expr.op.ToText(), expr.left, expr.right);
		}

		public string Visit_PrintStmt(PrintStmt stmt) {
			return Parenthesize("Print", stmt.expression);
		}

		public string Visit_ReturnStmt(ReturnStmt stmt) {
			return Parenthesize("Return", stmt.value);
		}

		public string Visit_SetExpr(SetExpr expr) {
			var name = (string) expr.name.value;
			var obj = Parenthesize("", expr.obj);
			var value = Parenthesize("", expr.value);
			return "(Set " + name + " on " + obj + " to " + value + ")";
		}

		public string Visit_SuperExpr(SuperExpr expr) {
			return "super";
		}

		public string Visit_ThisExpr(ThisExpr expr) {
			return "(Expr this)";
		}

		public string Visit_UnaryExpr(UnaryExpr expr) {
			return Parenthesize(expr.op.ToText(), expr.right);
		}

		public string Visit_VariableExpr(VariableExpr expr) {
			return "(Var " + (string)expr.name.value + ")";
		}

		public string Visit_VarStmt(VarStmt stmt) {
			return Parenthesize("VarDecl " + (string)stmt.name.value, stmt.initializer);
		}

		public string Visit_WhileStmt(WhileStmt stmt) {
			return Parenthesize("While", new ExpressionStmt(stmt.condition), stmt.body);
		}

		private string Parenthesize(string name, params Expr[] exprs) {
			StringBuilder sb = new StringBuilder();

			sb.Append("(").Append(name);
			foreach(var expr in exprs) {
				sb.Append(" ");
				sb.Append(expr.Accept(this));
			}
			sb.Append(")");

			return sb.ToString();
		}

		private string Parenthesize(string name, params Stmt[] stmts) {
			StringBuilder sb = new StringBuilder();

			sb.Append("(").Append(name);
			foreach(var stmt in stmts) {
				sb.Append(" ");
				sb.Append(stmt.Accept(this));
			}
			sb.Append(")");

			return sb.ToString();
		}
	}
}
